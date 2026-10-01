using System;
using UnityEngine;
using Unity.AI.Navigation;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Prefab modules use a centred unit-box contract: local X length, Y height, Z depth.
    /// All dressing is visual-only; the site's separate public blocker owns navigation.</summary>
    [CreateAssetMenu(menuName="SilverScreen/Construction Visual Kit")]
    public sealed class ConstructionVisualKit : ScriptableObject
    {
        [Serializable] private sealed class Module
        {
            public ConstructionModuleType Type;
            public GameObject Prefab;
        }
        [SerializeField] private Module[] _modules=Array.Empty<Module>();
        public GameObject Resolve(ConstructionModuleType type)
        {
            foreach(var module in _modules)if(module.Type==type&&module.Prefab!=null)return module.Prefab;
            return null;
        }
        public static GameObject Create(ConstructionModulePlacement module,Transform parent,Material sharedMaterial,ConstructionVisualKit kit)
        {
            var prefab=kit!=null?kit.Resolve(module.Type):null;
            GameObject root;
            if(prefab!=null)
            {
                root=Instantiate(prefab,parent,false);
                foreach(var collider in root.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                foreach(var behaviour in root.GetComponentsInChildren<Behaviour>(true))behaviour.enabled=false;
            }
            else
            {
                root=new GameObject(module.Type.ToString());root.transform.SetParent(parent,false);
                void Part(Vector3 position,Vector3 size,Color color)
                {
                    var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=module.Type.ToString();part.transform.SetParent(root.transform,false);
                    part.transform.localPosition=position;part.transform.localScale=size;
                    var collider=part.GetComponent<Collider>();collider.enabled=false;Release(collider);
                    var renderer=part.GetComponent<Renderer>();renderer.sharedMaterial=sharedMaterial;
                    var properties=new MaterialPropertyBlock();properties.SetColor("_BaseColor",color);renderer.SetPropertyBlock(properties);
                }
                var timber=new Color(.48f,.31f,.13f);var board=new Color(.69f,.51f,.28f);var metal=new Color(.28f,.30f,.27f);
                switch(module.Type)
                {
                    case ConstructionModuleType.Ladder:
                        Part(new Vector3(-.42f,0,0),new Vector3(.16f,1,1),timber);Part(new Vector3(.42f,0,0),new Vector3(.16f,1,1),timber);
                        for(int i=0;i<6;i++)Part(new Vector3(0,-.42f+i*.168f,0),new Vector3(.9f,.045f,1),board);break;
                    case ConstructionModuleType.TemporaryFence:
                        Part(new Vector3(-.46f,0,0),new Vector3(.06f,1,1),timber);Part(new Vector3(.46f,0,0),new Vector3(.06f,1,1),timber);
                        Part(new Vector3(0,.23f,0),new Vector3(1,.15f,1),board);Part(new Vector3(0,-.2f,0),new Vector3(1,.15f,1),board);break;
                    case ConstructionModuleType.TimberPile:
                        for(int y=0;y<3;y++)for(int x=0;x<3;x++)Part(new Vector3((x-1)*.32f,(y-1)*.3f,0),new Vector3(.28f,.25f,1),board);break;
                    case ConstructionModuleType.MaterialPile:
                    case ConstructionModuleType.DebrisPile:
                        for(int i=0;i<5;i++)Part(new Vector3((i%2==0?-.2f:.2f),-.35f+(i/2)*.24f,(i%3-1)*.2f),new Vector3(.55f,.25f,.55f),new Color(.52f,.48f,.40f));break;
                    case ConstructionModuleType.Crate:
                        Part(Vector3.zero,new Vector3(1,.95f,1),board);
                        Part(new Vector3(-.4f,0,-.49f),new Vector3(.12f,1,.02f),timber);Part(new Vector3(.4f,0,-.49f),new Vector3(.12f,1,.02f),timber);break;
                    case ConstructionModuleType.WheelbarrowPlaceholder:
                        Part(new Vector3(0,.1f,0),new Vector3(1,.5f,.7f),metal);
                        Part(new Vector3(-.3f,-.23f,.36f),new Vector3(.08f,.55f,.08f),timber);Part(new Vector3(.3f,-.23f,.36f),new Vector3(.08f,.55f,.08f),timber);
                        Part(new Vector3(0,-.28f,-.35f),new Vector3(.2f,.4f,.3f),metal);break;
                    case ConstructionModuleType.CanvasOrTarp: Part(Vector3.zero,Vector3.one,new Color(.62f,.62f,.42f));break;
                    default: Part(Vector3.zero,Vector3.one,module.Type==ConstructionModuleType.ScaffoldPlatform?board:timber);break;
                }
            }
            root.name=module.Id+" ["+module.Type+"]";
            root.transform.localPosition=module.Position;root.transform.localRotation=module.Rotation;root.transform.localScale=module.Size;
            return root;
        }
        internal static void Release(UnityEngine.Object item)
        {if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        public static void MarkVisualOnly(GameObject root)
        {var modifier=root.AddComponent<NavMeshModifier>();modifier.ignoreFromBuild=true;}
    }
}
