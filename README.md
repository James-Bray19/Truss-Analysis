# Truss Analyzer 2D

![image](https://github.com/James-Bray19/Truss-Analysis/assets/47334864/f5459351-9d87-40b6-9244-4c4cebb326fd)

## Overview

The Truss Analyzer 2D is a Unity-based software tool designed for analyzing 2D truss structures. It provides engineers and enthusiasts with a platform to visualize, analyze, and evaluate the behavior of truss systems under various loading conditions. This tool enables users to input truss configurations, material properties, and external loads, and then visualize the resulting deformation, stresses, and displacements within the structure.

## Features

1. **Node and Element Creation:** Users can create nodes and elements to construct truss structures. Nodes represent the connection points, while elements define the structural members between nodes.
2. **Material Selection:** The software allows users to choose from a range of material properties, including Young's modulus and yield stress, to simulate different material behaviors.
3. **Graphical Visualization:** The truss structure and its elements are graphically rendered within the Unity environment, providing users with a clear visual representation of the system.
4. **Force Input:** Users can input external forces acting on nodes, allowing for the simulation of various loading conditions.
5. **Interactive Node Manipulation:** Users can rearrange the structure by clicking and dragging nodes. Double pressing a node fixes or unfixed it, indicated by a change in color (dark grey means fixed).
6. **Deformation Model:** The software generates a deformation model, illustrating how the structure deforms under applied loads.
7. **Stress Analysis:** Users can visualize stress within the truss elements, with stress shown by a gradient of colors from no stress to the breaking point.
8. **Data Outputs:** The software calculates and displays relevant data, including total displacement, total stress, cross-sectional area, and total volume.

## How to Use

1. **Material Selector:**
   - Use the dropdown menu to select different material properties for elements.

2. **Cross-sectional Area Slider:**
   - Adjust the slider to change the width of the beams, assuming constant area.

3. **Interactive Node Manipulation:**
   - Click and drag nodes to rearrange the structure.
   - Double press a node to fix or unfixed it, indicated by a change in color.

4. **Model Visualization:**
   - Toggle checkboxes next to the model name to turn models on/off.
   - Deformation model shows the structure at static equilibrium.
   - Stress model shows stress with a gradient of colors from no stress to the breaking point.

5. **Data Outputs:**
   - Total displacement: The sum of the magnitude of all nodal displacements.
   - Total volume: The overall volume of material used before deformation.
   - Global stress: The magnitude of stress in the whole model after deformation.

## Limitations

- This software is admittedly an unfinished project before switching to topology optimisation for my A-Level project
- Please don't expect accuracy or user-friendly features, I just use this code to gauge how trusses roughly deform 
