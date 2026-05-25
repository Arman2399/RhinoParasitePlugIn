using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace ParasiticGrowth
{
    public class ParasiticGrowthCommand : Command
    {
        public override string EnglishName => "ParasiticGrowthCommand";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // 1. Select Host Brep
            GetObject goBrep = new GetObject();
            goBrep.SetCommandPrompt("Select the base Brep (host structure)");
            goBrep.GeometryFilter = Rhino.DocObjects.ObjectType.Brep;
            goBrep.SubObjectSelect = false;
            goBrep.Get();
            if (goBrep.CommandResult() != Result.Success) return goBrep.CommandResult();
            Brep hostBrep = goBrep.Object(0).Brep();

            // 2. Select Infection Origins
            GetObject goPoints = new GetObject();
            goPoints.SetCommandPrompt("Select Point3d objects as infection origins");
            goPoints.GeometryFilter = Rhino.DocObjects.ObjectType.Point;
            goPoints.GetMultiple(1, 0);
            if (goPoints.CommandResult() != Result.Success) return goPoints.CommandResult();

            List<Point3d> origins = new List<Point3d>();
            for (int i = 0; i < goPoints.ObjectCount; i++)
            {
                origins.Add(goPoints.Object(i).Point().Location);
            }

            // 3. Execution Parameters (Metric - mm/m depending on file units)
            int pointDensity = 2000;
            double killDistance = 2.0;
            double influenceDistance = 15.0;
            double segmentLength = 1.5;
            double maxRadius = 0.5;
            int maxIterations = 500;

            // Run Algorithm
            List<ScaNode> network = RunSpaceColonization(hostBrep, origins, pointDensity, killDistance, influenceDistance, segmentLength, maxIterations);

            // Generate Geometry
            GenerateGeometry(doc, network, maxRadius);

            doc.Views.Redraw();
            return Result.Success;
        }

        private List<ScaNode> RunSpaceColonization(Brep host, List<Point3d> origins, int targetPoints, double killDist, double influenceDist, double stepSize, int maxIters)
        {
            List<Point3d> attractors = GenerateAttractorsOnBrep(host, targetPoints);
            List<ScaNode> nodes = new List<ScaNode>();

            // Initialize roots
            foreach (var pt in origins)
            {
                // Snap origin strictly to the Brep
                host.ClosestPoint(pt, out Point3d snappedRoot, out _, out _, out _, 0.0, out _);
                nodes.Add(new ScaNode(snappedRoot, null, 0));
            }

            for (int iter = 0; iter < maxIters; iter++)
            {
                if (attractors.Count == 0) break;

                // Reset node influence
                foreach (var node in nodes)
                {
                    node.GrowthDir = Vector3d.Zero;
                    node.InfluenceCount = 0;
                }

                List<int> attractorsToRemove = new List<int>();

                // Phase 1: Associate attractors with the closest node
                for (int i = 0; i < attractors.Count; i++)
                {
                    Point3d attractor = attractors[i];
                    ScaNode closestNode = null;
                    double minDist = double.MaxValue;

                    foreach (var node in nodes)
                    {
                        double d = attractor.DistanceTo(node.Position);
                        if (d < minDist)
                        {
                            minDist = d;
                            closestNode = node;
                        }
                    }

                    if (closestNode != null)
                    {
                        if (minDist < killDist)
                        {
                            attractorsToRemove.Add(i);
                        }
                        else if (minDist < influenceDist)
                        {
                            Vector3d dir = attractor - closestNode.Position;
                            dir.Unitize();
                            closestNode.GrowthDir += dir;
                            closestNode.InfluenceCount++;
                        }
                    }
                }

                // Phase 2: Cull reached attractors (reverse order to preserve indices)
                for (int i = attractorsToRemove.Count - 1; i >= 0; i--)
                {
                    attractors.RemoveAt(attractorsToRemove[i]);
                }

                // Phase 3: Grow branches
                List<ScaNode> newNodes = new List<ScaNode>();
                foreach (var node in nodes)
                {
                    if (node.InfluenceCount > 0)
                    {
                        Vector3d avgDir = node.GrowthDir / node.InfluenceCount;
                        avgDir.Unitize();
                        Point3d rawNextPos = node.Position + (avgDir * stepSize);

                        // Constrain growth strictly to the host Brep surface
                        if (host.ClosestPoint(rawNextPos, out Point3d snappedPos, out _, out _, out _, 0.0, out _))
                        {
                            newNodes.Add(new ScaNode(snappedPos, node, node.Depth + 1));
                        }
                    }
                }

                if (newNodes.Count == 0) break; // Dead end reached
                nodes.AddRange(newNodes);
            }

            return nodes;
        }

        private List<Point3d> GenerateAttractorsOnBrep(Brep brep, int count)
        {
            List<Point3d> pts = new List<Point3d>();
            BoundingBox bbox = brep.GetBoundingBox(true);
            Random rnd = new Random();

            // Generate points inside the bounding box and pull them to the surface
            int attempts = 0;
            while (pts.Count < count && attempts < count * 10)
            {
                Point3d randomPt = new Point3d(
                    bbox.Min.X + rnd.NextDouble() * (bbox.Max.X - bbox.Min.X),
                    bbox.Min.Y + rnd.NextDouble() * (bbox.Max.Y - bbox.Min.Y),
                    bbox.Min.Z + rnd.NextDouble() * (bbox.Max.Z - bbox.Min.Z)
                );

                if (brep.ClosestPoint(randomPt, out Point3d closest, out _, out _, out _, 0.0, out _))
                {
                    pts.Add(closest);
                }
                attempts++;
            }
            return pts;
        }

        private void GenerateGeometry(RhinoDoc doc, List<ScaNode> nodes, double maxRadius)
        {
            double decayFactor = 0.05; // Controls how aggressively the branches thin out

            foreach (var node in nodes)
            {
                if (node.Parent == null) continue;

                LineCurve segment = new LineCurve(node.Parent.Position, node.Position);

                // Radius inversely proportional to branch depth
                double currentRadius = maxRadius / ((node.Depth * decayFactor) + 1);

                // Prevent inverted or microscopically thin pipes failing the Brep builder
                if (currentRadius < 0.01) currentRadius = 0.01;

                // Generate solid pipe
                Brep[] pipes = Brep.CreatePipe(
                    segment,
                    currentRadius,
                    false,
                    PipeCapMode.Round,
                    true,
                    doc.ModelAbsoluteTolerance,
                    doc.ModelAngleToleranceRadians
                );

                if (pipes != null && pipes.Length > 0)
                {
                    foreach (Brep pipe in pipes)
                    {
                        doc.Objects.AddBrep(pipe);
                    }
                }
                else
                {
                    // Fallback: output the curve if the pipe fails due to topology errors
                    doc.Objects.AddCurve(segment);
                }
            }
        }
    }

    // Node Data Structure
    public class ScaNode
    {
        public Point3d Position { get; set; }
        public ScaNode Parent { get; set; }
        public int Depth { get; set; }
        public Vector3d GrowthDir { get; set; }
        public int InfluenceCount { get; set; }

        public ScaNode(Point3d pos, ScaNode parent, int depth)
        {
            Position = pos;
            Parent = parent;
            Depth = depth;
            GrowthDir = Vector3d.Zero;
            InfluenceCount = 0;
        }
    }

   
    }