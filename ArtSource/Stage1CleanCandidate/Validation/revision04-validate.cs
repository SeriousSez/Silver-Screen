using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using System.Linq;using SilverScreen.Editor;using SilverScreen.Presentation.Buildings;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){
var root=Stage1CandidateReview.FindReviewObject("Stage1_CleanCandidate_A_REVIEW_ONLY");
int missing=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
var meshes=root.GetComponentsInChildren<MeshFilter>();int tris=0;int invalid=0;
foreach(var f in meshes){var m=f.sharedMesh;if(m==null)throw new System.Exception("Missing mesh "+f.name);tris+=m.triangles.Length/3;if(f.name.StartsWith("Export_")&&(m.uv.Length!=m.vertexCount||m.normals.Length!=m.vertexCount||m.tangents.Length!=m.vertexCount)){invalid++;result.Log("Mesh channels incomplete: "+f.name);}}
var renderers=root.GetComponentsInChildren<Renderer>();var materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
foreach(var m in materials)if(m==null||m.shader==null||m.shader.name.Contains("Error"))throw new System.Exception("Invalid material");
var sign=root.GetComponentInChildren<StageArchitecturalSign>();var identity=root.GetComponent<SoundStageIdentity>();int one=sign.GetComponent<MeshFilter>().sharedMesh.vertexCount;sign.SetNumber(2);int two=sign.GetComponent<MeshFilter>().sharedMesh.vertexCount;identity.RefreshSign();
if(one==two||identity.StageNumber!=1)throw new System.Exception("Instance signage failed");
Physics.SyncTransforms();var cols=root.GetComponentsInChildren<MeshCollider>();int centralHits=0,floorHits=0;
foreach(var c in cols){if(c.sharedMesh==null)throw new System.Exception("Missing collider mesh");if(c.Raycast(new Ray(root.transform.TransformPoint(new Vector3(0,1,-10)),Vector3.forward),out var hit,20))centralHits++;if(c.Raycast(new Ray(root.transform.position+Vector3.up,Vector3.down),out hit,2)){floorHits++;result.Log("Floor hit "+hit.point);}}
var walls=cols.First(c=>c.name.Contains("ExteriorWalls"));foreach(float x in new[]{0f,-5.97f,5.70f})if(walls.Raycast(new Ray(root.transform.TransformPoint(new Vector3(x,1,-13)),Vector3.forward),out var hit,2))throw new System.Exception("Masonry blocks opening "+x);
if(missing!=0||invalid!=0||centralHits!=0||floorHits!=1||cols.Length!=5)throw new System.Exception("Candidate integrity failed");
var probe=root.GetComponentInChildren<ReflectionProbe>();if(probe==null||probe.customBakedTexture==null)throw new System.Exception("Missing interior reflection");
foreach(int side in new[]{-1,1})foreach(float y in new[]{-10.025f,10.025f})foreach(float z in new[]{.20f,1f,2.15f})foreach(float u in new[]{-.40f,0,.40f})
if(walls.Raycast(new Ray(root.transform.TransformPoint(new Vector3(side*9,z,y+u)),root.transform.TransformDirection(new Vector3(-side,0,0))),out var hit,2))throw new System.Exception("Side masonry aperture blocked");
foreach(float x in new[]{-.60f,0,.60f})foreach(float z in new[]{.2f,1f,2.5f})if(walls.Raycast(new Ray(root.transform.TransformPoint(new Vector3(x,z,13)),Vector3.back),out var hit,2))throw new System.Exception("Rear masonry aperture blocked");
foreach(var r in renderers.Where(r=>r.name=="Export_Roof"||r.name=="Export_RoofGlazing"))if(r.probeAnchor==null||Mathf.Abs(r.probeAnchor.localPosition.y-13)>.001f)throw new System.Exception("Roof does not sample exterior reflection anchor");
var roof=materials.First(m=>m.name=="C1_Roof");if(Mathf.Abs(roof.GetFloat("_Metallic")-.78f)>.001f||roof.GetTexture("_BaseMap").name!="roof_metal_albedo")throw new System.Exception("Roof material import mismatch");
var bounds=new Bounds(root.transform.position,Vector3.zero);foreach(var r in renderers)bounds.Encapsulate(r.bounds);
if(Mathf.Abs(bounds.min.y-root.transform.position.y)>.00002f)throw new System.Exception("Imported ground datum mismatch "+bounds.min);
result.Log("CHECK importedGroundDatum="+bounds.min.y.ToString("F6")+" sideOpeningRays=36 rearOpeningRays=9 roofExteriorProbeAnchors=2 roofMetallic="+roof.GetFloat("_Metallic")+" roofSmoothness="+roof.GetFloat("_Smoothness"));
if(root.GetComponentsInChildren<Light>(true).Length!=31)throw new System.Exception("Duplicate review lights");
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Stage1CandidateReview.PrefabPath);if(prefab.GetComponentsInChildren<Light>(true).Length!=31)throw new System.Exception("Duplicate saved lights");
foreach(var candidate in SceneManager.GetActiveScene().GetRootGameObjects())if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate)==Stage1CandidateReview.PrefabPath) {
if(candidate.GetComponentsInChildren<Light>(true).Length!=31)throw new System.Exception("Duplicate live candidate lights");
result.Log("Live candidate root="+candidate.transform.position.ToString("F6")+" lights=31");}
var live=SceneManager.GetActiveScene();
result.Log("CHECK missingScripts="+missing+" importedMeshChannelsInvalid="+invalid+" renderers="+renderers.Length+" trianglesWithSign="+tris+" distinctMaterials="+materials.Length+" colliders="+cols.Length+" centralHits="+centralHits+" floorHits="+floorHits+" stage1GlyphVertices="+one+" stage2GlyphVertices="+two+" stageRestored="+identity.StageNumber);
result.Log("Permanent spot lights="+root.GetComponentsInChildren<Light>().Length+" reflection="+probe.customBakedTexture.name+" cubemapSize="+probe.resolution+" productionRegistration="+(root.GetComponent<StudioBuildingView>()!=null));
result.Log("Editor Unity="+Application.unityVersion+" compiling="+EditorApplication.isCompiling+" playing="+EditorApplication.isPlaying+" target="+EditorUserBuildSettings.activeBuildTarget+" active="+live.path+" dirty="+live.isDirty);
result.Log("Review outside build settings="+!EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==Stage1CandidateReview.ReviewPath));
result.Log("Original Studio has not been saved or reloaded. Its extra candidate was seated at measured ground as one undoable root change. Play Mode, navigation, door movement, stair collision, player build and performance benchmark not exercised.");
}}

