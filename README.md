# HoloLens 2 AR Visualization of Neurovascular Aneurysm

This project was developed by 4th-year Computer Science students Connor Haines, Justin Weller, Finn Ferguson, and XuanRan Qi at Western University under the supervision of Dr. Elvis Chen at Robarts Research Institute in 2022-23.

**Video Demo:** https://www.youtube.com/watch?v=BPGqZ186OqE

**Side by Side Demo:** https://www.youtube.com/watch?v=eiuOeGblh0M

## Project Description
This project aims to improve surgeon and patient communication by using augmented reality (AR) technology to display 3D holograms where in the past 2D images would otherwise be used. The goal is to display a medical model on multiple HoloLens 2 devices at the same time, located in the same place in the local environment, with interactions synced between all devices. This makes it much easier for humans to interpret 3D medical scans.

Microsoft HoloLens 2 is utilized along with supporting software systems such as Unity, MRTK, Azure Spatial Anchors (ASA), and Photon Networking (PUN). The intention is that raw medical scans (MRI, CT) should first be pre-processed by other external teams with 3DSlicer (automated segmentation), which will produce a 3D .obj model of the medical image, to then be imported into the software.

With the inclusion of the STL importer, it should be possible for STL files to be transferred from a PC to the Hololens via USB. As long as the user is signed into the Hololens, the STL files can be transferred to the 3D objects folder on the Hololens. 

**Azure Spatial Anchors (ASA)** are used for establishing a common coordinate system between multiple devices in the same environment. 

**Photon Unity Networking (PUN)** is used to synchronize interactions with 3D models between devices, as well as any required data sharing.

## Technical Requirements
- Microsoft HoloLens 2 (in developer mode)
- Unity 2020.3.4
- Visual Studio 2019
- Wifi Internet connection
- Mixed Reality Tool Kit (MRTK) 2.8.2
- Set up & Installation
- Clone the project from the git repo
- Ensure that all software is updated to match the above technical requirements.
- Open the project in Unity (2020.3.4) and perform any required setup
- Ensure that all packages are successfully installed in the Unity Package Manager:

**Note** that there are also 2 packages installed in \Packages\spatial-anchors-sdk
These should be fine, but if needed they can be seen in \Packages\manifest.json

## Structure
The system uses a combination of Azure Spatial Anchors and Photon Unity Networking to establish a common local coordinate system and synchronized interactions.

Device 1 creates an Azure Spatial Anchor, receives an AnchorId from the Azure resource, and shares it with the Photon Lobby. Device 2 then requests the Anchor using the given AnchorId. When both devices have the same anchor, the system relocates itself to the location of the anchor, so that both devices see the same objects in the same place.

Photon is also used to synchronize the interactions and translations of the objects across the devices. For example, if Device 1 grabs and moves an object, Device 2 will also see that same movement.

## Azure Spatial Anchors (ASA)
Documentation: https://learn.microsoft.com/en-us/azure/spatial-anchors/
To establish a common local coordinate system, we use a Microsoft Azure resource called Spatial Anchors.

This system uses local environment data from the various sensors and cameras and stores the position of an “anchor” relative to the environment in the cloud. Any HoloLens can then request the anchor using a variety of methods, though our system simply requests by ID. 

This is effective in establishing a common local coordinate system for multiple HoloLenses in the same environment.

**Note:** An Azure account and Spatial Anchors resource must be created and the **Account ID**, **Account Key**, and **Account Domain** keys must be updated in the SpatialAnchorManager.

## Photon Unity Networking (PUN)
Documentation: https://doc.photonengine.com/pun/current/getting-started/pun-intro

Tutorial Used for Setup: https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/tutorials/mr-learning-sharing-01

Photon Networking is used to synchronize interactions with the 3D model being shared across multiple HoloLens devices. The PhotonView script is applied to the model which makes it a Networked Object and instantiates it in the Photon Lobby for all devices. 
Photon is also used to share the ASA AnchorId between devices. For example, when an Anchor is created by Device 1, the Azure resource returns a unique AnchorId (UUID format), which Device 1 then shares with the Photon Lobby. Device 2 then requests the anchor using the shared AnchorId.

**Notes:** 
- A new Photon account and App must be set up and the Photon AppId must be updated in the source code.
- Under the “Manage” page of the App on the Photon portal, we White Listed the Canada East region (‘cae’).
  - This was required because Photon will only connect to what it thinks is the nearest region/server, which can sometimes result in discrepancies where both HoloLenses are in different regions and therefore different Lobbies
  - Even though we hardcoded the Lobby name (“Capstone4470” I believe), Photon allows for multiple lobbies with the same name in different regions, which is why White Listing is necessary.

## Build & Deployment
In Unity:

**Note:** Ensure Holographic Remoting has been disabled before attempting to deploy (Project Settings > OpenXR and under the UWP settings tab disable “Holographic Remoting remote app”. While it is a useful tool, Holographic remoting does not work with ASA.
1) File > Build Settings
2) Build with the following UWPsettings:
3) Select a Folder to create the build in
4) Open the .sln solution file in Visual Studio
In Visual Studio:
5) Plug HoloLens 2 device into PC via USB
6) Select Debug or Release (recommended)
7) Select ARM64
8) Select Device

9) Debug tab > Start Without Debugging (with device signed into main menu)

10) Wait for it to Compile, Build, and Deploy
  - This will deploy the build locally onto the HL2, so it can now be unplugged
**Note:** For debug logs, select Debug and Debug>Start With Debugging. A long USB cable is recommended, as the device must remain plugged in to show the logs.

Steps: https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/build-and-deploy-to-hololens

**Note:** The 3D .obj file located at [/Assets/Models/](https://github.com/connor2033/hololens-aneurysm-visualization/tree/main/Assets/Models) is a **decimated** (downsized) version to fit within GitHub's file size limitations. We have included a **zipped/compressed version** of the **original file** that can be extracted and used to replaced the currently existing one. Simply rename it to replace the current obj.

## Running
How to run the app:
1) Devices 1 and 2 start the app
2) Both devices wait to load into the networked Photon lobby (model appears)
3) Device 1 creates anchor (either with button or voice), white Anchor sphere appears
4) Wait for anchor to upload to Azure resource (sphere will turn green)
5) Both devices Load Anchor (button or voice)
6) Enjoy!

Note that the app also works for people in different locations without the Anchor synchronization, which is the reason for the sphere (halo)  above people’s heads.
