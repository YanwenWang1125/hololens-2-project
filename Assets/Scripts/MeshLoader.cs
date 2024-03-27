//David Hocking's code integrated in
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.XR.ARFoundation;
using MRTK.Tutorials.MultiUserCapabilities;
using Microsoft.MixedReality.Toolkit.UI;
using Photon.Pun;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.Rendering;
using System.Threading.Tasks;
using TMPro;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;

#if WINDOWS_UWP // Only have these namespaces if on UWP devices
using Windows.Storage; 
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class MeshLoader : MonoBehaviour
{
    public Material objectMaterial;
    public GameObject dynamicDescription;
    private string oldText;
    private string newestSTL = null;
    //private PhotonRoom photonRoom;

    // Start is called before the first frame update
    void Start()
    {
        LoadModel();
    }

    private Mesh LoadModel()
    {
        Mesh mesh = null;
#if WINDOWS_UWP
        //this snippet runs when deployed as an app on Hololens

        //intention is that model is already loaded into Hololens through USB, (sign into Hololens while/before connecting, will have access to 3D Objects folder)
        
        string folder = Windows.Storage.KnownFolders.Objects3D.Path; //accesses 3D Objects folder on Hololens
        newestSTL = FindNewestSTL(folder); //returns path of the newest STL file in folder
        if (newestSTL != null) { 

        mesh = ReadPathSTL(newestSTL);
        ChangeDescription("File content loaded successfully.");

         }
        else
        {
            ChangeDescription("Unable to find stl file at "+folder);
            return null;
        }

#else
        // this snippet runs when testing on Unity Engine
        string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        newestSTL = FindNewestSTL(folder);
        if (newestSTL != null)
        {
            //string path = Path.GetFullPath(Path.Combine(folder, newestSTL)); //Get Full Path ensures path is consistent ( / and \ are mixed otherwise)
            //string path = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, name)); //gets stl from inside unity app
            byte[] array = File.ReadAllBytes(newestSTL); // don't need this if reading from path
            mesh = ReadByteSTL(array);
            //Mesh mesh = ReadPathSTL(path);
        }
        else
        {
            ChangeDescription("Unable to find stl file at " + folder);
            return null;
        }
#endif
        if (mesh != null)
        {
            gameObject.SetActive(true); // Make sure the GameObject (that the script is attached to) is active

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = mesh;

            // Use the custom material if it has been assigned in the editor
            if (objectMaterial != null)
            {
                meshRenderer.material = objectMaterial;
            }
            else
            {
                Debug.Log("No material set, using standard material");
                // Fallback to a default material if no custom material is provided
                meshRenderer.material = new Material(Shader.Find("Standard"));
            }

            // Add components to GameObject
            AddComponents(gameObject, mesh);

            ChangeDescription("Loaded STL file: " + name);
            //Debug.Log("Loaded STL file: " + name);
            return mesh;
        }
        else
        {
            ChangeDescription("Failed to load STL file.");
            //Debug.LogError("Failed to load STL file.");
            return null;
        }


    }
    private void ChangeDescription(string updateText)
    {
        TextMeshPro textMeshProComponent = dynamicDescription.GetComponent<TextMeshPro>();

        if (textMeshProComponent != null)
        {
            oldText = textMeshProComponent.text;
            updateText = oldText + updateText;
            textMeshProComponent.text = updateText;
        }
        else
        {
            Debug.LogError("No TextMeshProUGUI component found on " + dynamicDescription.name);
        }
    }

    private string FindNewestSTL(string path)
    {
        ChangeDescription("Finding the newest stl file... ");
        FileInfo recentFile = new DirectoryInfo(path).GetFiles("*.stl").OrderByDescending(o => o.LastWriteTime).FirstOrDefault();
        ChangeDescription("file found: " + recentFile.FullName);
        return recentFile.FullName;
    }
    private void AddComponents(GameObject meshObject, Mesh mesh)
    {
        try
        {
            AddBoxCollider(meshObject, mesh);
            meshObject.AddComponent<TableAnchorAsParent>();
            PhotonView pv = meshObject.AddComponent<PhotonView>();
            pv.ViewID = 5;
            meshObject.AddComponent<PhotonTransformView>();
            meshObject.AddComponent<GenericNetSync>();
            meshObject.AddComponent<OwnershipHandler>();
            meshObject.AddComponent<NearInteractionGrabbable>();
            meshObject.AddComponent<ConstraintManager>();
            meshObject.AddComponent<ObjectManipulator>();

            VoiceTransformController voiceTransform = meshObject.AddComponent<VoiceTransformController>();
            voiceTransform.obj = meshObject;
            BoundsControl bounds = meshObject.AddComponent<BoundsControl>();
            bounds.BoundsControlActivation = BoundsControlActivationType.ActivateManually;

        }
        catch (Exception ex)
        {
            Debug.LogWarning("Error adding components: " + ex.Message);
            ChangeDescription("Error adding components: " + ex.Message);
        }
    }

    private void AddBoxCollider(GameObject meshObject, Mesh mesh)
    {
        BoxCollider collider = meshObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = meshObject.AddComponent<BoxCollider>();
        }
        collider.center = mesh.bounds.center;
        collider.size = mesh.bounds.size;
    }

    private Mesh ReadPathSTL(string path)
    {
        if (!File.Exists(path))
        {
            ChangeDescription("File not found: " + path);
            Debug.LogError("File not found: " + path);
            return null;
        }

        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    //Skip header
                    br.ReadBytes(80);

                    //Read # of triangles
                    uint triangleCount = br.ReadUInt32();


                    Vector3[] vertices = new Vector3[triangleCount * 3];
                    int[] triangles = new int[triangleCount * 3];



                    int i = 0;
                    //Read triangles
                    for (; i < triangleCount; i++)
                    {
                        //Skip normal
                        br.ReadBytes(12);

                        float baseScale = 0.01f;

                        // Read vertices
                        for (int j = 0; j < 3; j++)
                        {
                            float x = br.ReadSingle() * baseScale;
                            float y = br.ReadSingle() * baseScale;
                            float z = br.ReadSingle() * baseScale;
                            vertices[i * 3 + j] = new Vector3(x, y, z);
                        }

                        //skip attribute
                        br.ReadUInt16();

                        //Setup triangles
                        triangles[i * 3] = i * 3;
                        triangles[i * 3 + 1] = i * 3 + 1;
                        triangles[i * 3 + 2] = i * 3 + 2;
                    }


                    Mesh mesh = new Mesh();
                    mesh.indexFormat = IndexFormat.UInt32;
                    mesh.vertices = vertices;
                    mesh.triangles = triangles;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();

                    return mesh;
                }
            }
        }
        catch (IOException e)
        {
            Debug.LogError("Error reading STL file: " + e.Message);
            return null;
        }
    }

    private Mesh ReadByteSTL(byte[] data) //if using the tcp transfer, use this probably?
    {
        if (data == null)
        {
            Debug.LogError("No data packet received");
            ChangeDescription("No data packet received");
            return null;
        }

        try
        {

            //Skip header
            int indexCounter = 80;

            //Read # of triangles
            uint triangleCount = BitConverter.ToUInt32(data, indexCounter);
            indexCounter += sizeof(uint);

            Vector3[] vertices = new Vector3[triangleCount * 3];
            int[] triangles = new int[triangleCount * 3];

            //Read triangles
            for (int i = 0; i < triangleCount; i++)
            {
                //Skip normal
                indexCounter += 12;

                float baseScale = 0.01f;

                // Read vertices
                for (int j = 0; j < 3; j++)
                {
                    float x = BitConverter.ToSingle(data, indexCounter) * baseScale;
                    indexCounter += sizeof(float);
                    float y = BitConverter.ToSingle(data, indexCounter) * baseScale;
                    indexCounter += sizeof(float);
                    float z = BitConverter.ToSingle(data, indexCounter) * baseScale;
                    indexCounter += sizeof(float);
                    vertices[i * 3 + j] = new Vector3(x, y, z);
                }

                //skip attribute
                indexCounter += sizeof(UInt16);

                //Setup triangles
                triangles[i * 3] = i * 3;
                triangles[i * 3 + 1] = i * 3 + 1;
                triangles[i * 3 + 2] = i * 3 + 2;
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            ChangeDescription("Byte array processed, mesh returning. ");
            return mesh;
        }
        catch (IOException e)
        {
            Debug.LogError("Error reading STL file: " + e.Message);
            ChangeDescription("Error reading STL file: " + e.Message);
            return null;
        }
    }
}
