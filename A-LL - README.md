# Utils Core package of A-LL Creative Technology mobile Unity Framework

## Installation

To use this package in a unity project :

1. Clone the repository in local directory.
2. In Unity, open Window > Package Manager and "Add Package from git url ..." and insert this URL https://github.com/A-LL-Creative-Technology/A-LL-Core---Utils.

3. Manually install Firebase
    1. Download the firebase SDKs “Firebase Cloud Messaging” and “Google Analytics for Firebase” for Unity : https://developers.google.com/unity/archive
    2. In Unity editor, right-click on Assets then *Import Package* -> *Custom Package...* to import both packages.
4. Add the following third-party packages from the Package Manager
    1. Ask Laurent to give you access to the packages in Unity
    2. Select "My Assets" in the Package Manager to display paid Packages from the Asset Store
    3. Select and import all these packages
        - Rest Client for Unity 
            1. Additionnaly, create an Assembly Definition Reference named "Main Proyecto26" in "RestClient/Packages"
            2. Link it to Assembly A-LL.Core.Runtime.asmdef
5. Copy/Paste files in "A-LL Core - Utils/Assets To Copy/A-LL/Config/Firebase/" and paste them to "Assets/A-LL/Config/Firebase/" (create this path) and rename by removing the prefix "Sandbox - "