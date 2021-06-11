# HoloNav: A Mixed Reality IndoorNavigation System
## Introduction
Wayfinding indoors still has not been studied to the same extent as wayfind-ing outdoors. Though some approaches have been explored in recent studies forindoor navigation, no standard methods exist. Motivated recently-emersed Augmented Reality and Mixed Reality technologies, this research demonstrates a feasible workflow of developing a Mixed Reality indoor navigation system, HoloNav,on Microsoft HoloLens 2. The system utilized Azure Spatial Anchors as virtualmarkers for labeling landmarks. A user study was conducted on the two variantsof HoloNav designed with two different navigation visualizations to help gaininformation about the kind of visualizations preferred in a Mixed Reality navigation system. Furthermore, the benefits of Mixed Reality navigation systemscompared to traditional systems are also highlighted and discussed via a user study with more than twenty users.

[![HoloNav](https://img.youtube.com/vi/Gzxj2bzMrBU/0.jpg)](https://www.youtube.com/watch?v=Gzxj2bzMrBU)

For more details about the project, checkout our [report](IPA_report.pdf)
### Main functionalities
- Mapping
  - Create new anchors of interest in the environment
  - Populate auxillary information
- Navigation 
  - Choose from the created anchors an origin and a destination. 
  - Generate the shortest path between these two anchors using Dijkstra’s algorithm
  - Render the navigation visualizations indicating the route
  - Two navigation visualizations supported
### Two design variations
1. Hand-invoked minimap + arrows as navigation visualization
2. Static minimap + transition lines as navigation visualization
### Highlights
HoloNav leverages ESRI CityEngine and Azure Spatial Anchor to provide future solution for indoor navigation. Powered by Microsoft HoloLens 2 and its advanced Mixed Reality (MR) features, HoloNav enables eﬃcient and eﬀective indoor localization and navigation as well as direct manipulation with the hand-invoked mini-map, contributing to interactive and immersive user experiences. The application explores possible interaction concepts and visualizations of MR-based indoor navigation systems on mobile devices and demonstrates a working prototypes that can be extended to further applications.
## Dependencies
### Hardware
- Microsoft HoloLens 2
### Software
- MRTK v2.5.1
- Azure Spatial Anchor SDK version 2.7.0
- Unity 2019.4.12f1
- Visual Studio 2019 Version 16.5.0
> There is a known issue with the FollowMe/Solver Handler function of MRTK on HoloLens 2 with Visual Studio 2019 of version 16.5.0. This issue is claimed to be fixed with the next Visual Studio release, while prior this release, a walkaround would be using MRTK v2.5.0/v2.5.1 (or higher) Foundation together with Tools packages. 
> For previous version of Azure Spatial Anchor SDK, where the account domain is not required, there might be problems. Please use the latest version of SDK for better consistency.
## Setup the app with API Keys
### Azure Spatial Anchor
1. To access Azure Spatial Anchor, API keys are required, which can be acquired following the tutorial [***Create Azure Spatial Anchor***](https://docs.microsoft.com/en-us/azure/spatial-anchors/quickstarts/get-started-unity-hololens?tabs=azure-portal).
2. After the access to Azure Spatial Anchor is settled, corresponding information needs to be added to the project in Unity.
  - select "Anchor Manager" from the hierarchy, populate the information in the "Spatial Anchor Manager" in the inspector
  - set the "Spatial Anchors Account Id"
  - set the "Spatial Anchors Account Key"
  - set the "Spatial Anchors Account Domain"
### Azure Table Storage
1. Our app uses Azure Table Storage as the backend to store information about the anchors. To enable the access, follow the official tutorial [***Create Azure Table Account***](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-portal).
2. After setting up your Azure Table Storage, the following information is needed.
  - Open the project with unity, from the hierarchy, select "Data Manager" then input the "Connection String"
  - Specify the "Spatial Anchor Table Name", which will store all the auxillary information about the anchors to be created
  - Specify the "Adjacent List Table Name", which will store the spatial relationship between the two connected anchors
> Note: Azure Table Storage is a free service offer by Azure, which is sufficient for this project, yet Azure does also offer more advanced storage service *Azure Cosmos DB*. In addition, according to the official documentation, the Table API is integrated to Cosmos DB SDK in the newer version.
## Useful links
Our application is partially adapted based on the following official tutorials.
- To get around with Azure Spatial Anchor, we suggest checking out the HoloLens's official tutorials on [***Azure Spatial Anchors***](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/tutorials/mr-learning-asa-01).
- For integrating Azure Cloud Services into the app, we found the tutorials on [***Azure Cloud Services for HoloLens 2***](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/tutorials/mr-learning-azure-01) very useful.
## Authors
Tianyu Wu, Laura Schalbette, and Xavier Brunnner

In completion of Interdisciplinary Project Thesis 2019, ETH Zurich
