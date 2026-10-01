var root=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(SilverScreen.Editor.EnvironmentArt.StageSchoolB2Builder.Prefab);
var infrastructure=root.GetComponent<SilverScreen.Presentation.Buildings.StageSchoolInfrastructure>();
var lines=new System.Collections.Generic.List<string>();
lines.Add("anchor,x,y,z");
foreach(var a in infrastructure.Anchors){var p=root.transform.InverseTransformPoint(a.position);lines.Add(a.name+","+p.x+","+p.y+","+p.z);}
System.IO.File.WriteAllLines("ArtReview/StageSchoolB2/anchors.csv",lines);
System.IO.File.WriteAllText("ArtReview/StageSchoolB2/infrastructure.json",UnityEngine.JsonUtility.ToJson(infrastructure,true));
var groups=root.GetComponent<SilverScreen.Presentation.Buildings.BuildingCutawayController>().Groups;
lines.Clear();lines.Add("id,support,roof,renderers");
foreach(var g in groups)lines.Add(g.Id+","+g.SupportGroupId+","+g.Roof+","+g.Renderers.Length);
System.IO.File.WriteAllLines("ArtReview/StageSchoolB2/visibility_groups.csv",lines);
var settings=UnityEngine.AI.NavMesh.GetSettingsByID(0);
string audit="Unity="+UnityEngine.Application.unityVersion+"; pipeline="+UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name+"; compilationFailed="+UnityEditor.EditorUtility.scriptCompilationFailed+"; boxColliders="+root.GetComponentsInChildren<UnityEngine.BoxCollider>(true).Length+"; meshColliders="+root.GetComponentsInChildren<UnityEngine.MeshCollider>(true).Length+"; doors="+root.GetComponentsInChildren<SilverScreen.Presentation.Buildings.AuthoredBuildingDoor>(true).Length+"; anchors="+infrastructure.Anchors.Count+"; regions="+infrastructure.Regions.Count+"; agentRadius="+settings.agentRadius;
System.IO.File.WriteAllText("ArtReview/StageSchoolB2/prefab_audit.txt",audit);result.Log(audit);
