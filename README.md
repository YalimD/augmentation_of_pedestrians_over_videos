# Augmenting Virtual Agents into Real Crowd Videos Using Unity

This project aims to augment a virtual crowd into a real one extracted from given video.
The project is written on Unity using C#.

For collision detection between virtual agents and the static and dynamic obstacles (including real pedestrians) we use Reciprocal Velocity Obstacles (RVO) proposed by Dinesh Manocha et al.

We use Unity's PathFinding library for global path planning of virtual agents.

If you find this work useful, please cite:

```
@article{DOGAN2021141,
title = {An augmented crowd simulation system using automatic determination of navigable areas},
journal = {Computers & Graphics},
volume = {95},
pages = {141-155},
year = {2021},
issn = {0097-8493},
doi = {https://doi.org/10.1016/j.cag.2021.01.012},
url = {https://www.sciencedirect.com/science/article/pii/S0097849321000121},
author = {Yalım Doğan and Sinan Sonlu and Uğur Güdükbay},
keywords = {Pedestrian detection and tracking, Data-driven simulation, Three-dimensional reconstruction, Crowd simulation, Augmented reality, Deep learning},
abstract = {Crowd simulations imitate the group dynamics of individuals in different environments. Applications in entertainment, security, and education require augmenting simulated crowds into videos of real people. In such cases, virtual agents should realistically interact with the environment and the people in the video. One component of this augmentation task is determining the navigable regions in the video. In this work, we utilize semantic segmentation and pedestrian detection to automatically locate and reconstruct the navigable regions of surveillance-like videos. We place the resulting flat mesh into our 3D crowd simulation environment to integrate virtual agents that navigate inside the video avoiding collision with real pedestrians and other virtual agents. We report the performance of our open-source system using real-life surveillance videos, based on the accuracy of the automatically determined navigable regions and camera configuration. We show that our system generates accurate navigable regions for realistic augmented crowd simulations.}
}
```
