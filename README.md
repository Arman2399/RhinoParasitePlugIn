# Parasitic Architecture (Rhino 8 Plugin)

Translating the oppressive, decaying atmosphere of an abandoned rural town or the psychological horror of Silent Hill f. This tool does not design the primary building; it generates a suffocating secondary layer. It uses a space colonization algorithm to grow erratic, organic structural members over pristine geometry, creating a feeling that the architecture is being swallowed.

## Visual Documentation

### The Host
<img width="1783" height="1084" alt="TowerBefore" src="https://github.com/user-attachments/assets/94a299af-6ec6-4c99-b072-686b0f2bfa84" />
*Pristine base geometry prior to infection.*

### Growth Process
<p align="center">
  <img src="https://github.com/user-attachments/assets/2d344a29-8c12-41ad-9ed2-b2a37cc397c8" width="30%" />
  <img src="https://github.com/user-attachments/assets/94e70b05-cbdd-4384-b979-a4214ed6d9de" width="30%" />
  <img src="https://github.com/user-attachments/assets/471d4e60-e511-47c6-b484-5b7d1918f57b" width="30%" />
</p>
*Attractor distribution, vector calculation, and surface-constrained branch generation.*

### The Infection
<img width="1783" height="1084" alt="TowerPostCompressed" src="https://github.com/user-attachments/assets/0a30bb5e-4640-4f0a-a953-9aa6d1358cb6" />
*Final output: Geometry enveloped by the space colonization algorithm.*

---

## Prerequisites
* **Rhinoceros 8** (Minimum)
* Windows OS (Not tested on Mac)

## Installation
1. Download the latest `ParasiticArchitecture.rhp` release.
2. Right-click the `.rhp` file, select **Properties**, and check **Unblock** if applicable.
3. Open Rhino 8.
4. Drag and drop the `.rhp` file into the Rhino viewport, or install it via the `PluginManager`.

## Usage
1. Model or import your host geometry. **Ensure it is a single, closed Brep or boolean-unioned surface.**
2. Place `Point` objects in the Rhino viewport to act as the infection origins.
3. Type `ParasiticGrowth` into the command line.
4. Prompt 1: Select the base Brep. Press Enter.
5. Prompt 2: Select the infection origin points. Press Enter.

## Performance Notes
Generating dense, solid Brep networks is computationally intensive. The algorithm bounds growth to the host surface. Extreme point densities or deep iteration limits may cause Rhino to freeze while calculating the Brep pipes. It is recommended to test on smaller subsections of geometry before applying the command to large-scale urban or architectural models.
