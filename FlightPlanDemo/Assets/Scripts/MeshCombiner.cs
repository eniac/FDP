using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner : MonoBehaviour
{
    static public void Combine(GameObject go){
        //////////////////////////////////////////////////////////////////
        // Quaternion oldRot = go.transform.rotation;
        // Vector3 oldPos = go.transform.position;

        // go.transform.rotation = Quaternion.identity;
        // go.transform.position = Vector3.zero;

        // MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
        // Debug.Log("*************Child meshes = " + filters.Length);
        // Mesh finalMesh = new Mesh();
        // CombineInstance[] combiners = new CombineInstance[filters.Length];
        

        // for(int a=0; a<filters.Length; a++){
        //     if(filters[a].transform == go.transform){
        //         continue;
        //     }
        //     combiners[a].subMeshIndex = 0;
        //     combiners[a].mesh = filters[a].sharedMesh;
        //     combiners[a].transform = filters[a].transform.localToWorldMatrix;
        // }
        // finalMesh.CombineMeshes(combiners);

        // go.transform.GetComponent<MeshFilter>().sharedMesh = finalMesh;

        // go.transform.rotation = oldRot;
        // go.transform.position = oldPos;

        // for(int a=0; a<go.transform.childCount; a++){
        //     go.transform.GetChild(a).gameObject.SetActive(false);
        // }

        /////////////////////////////////////////////////////////////////

        MeshFilter myMeshFilter = go.transform.GetComponent<MeshFilter>();
        Mesh mesh = myMeshFilter.sharedMesh;
        if(mesh==null){
            mesh = new Mesh();
            myMeshFilter.sharedMesh = mesh;
        }
        else{
            mesh.Clear();
        }

        MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(false);
        Debug.Log("*************Child meshes = " + filters.Length);

        List<CombineInstance> combiners = new List<CombineInstance>();

        foreach(MeshFilter filter in filters){
            if(filter == myMeshFilter){
                continue;
            }

            CombineInstance ci = new CombineInstance();
            ci.mesh = filter.sharedMesh;
            ci.subMeshIndex = 0;
            ci.transform = Matrix4x4.identity;
            combiners.Add(ci);
        }
        mesh.CombineMeshes(combiners.ToArray(), false);

        /////////////////////////////////////////////////////////////////

        // MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
        // CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        // int i = 0;
        // while (i < meshFilters.Length)
        // {
        //     combine[i].mesh = meshFilters[i].sharedMesh;
        //     combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        //     // meshFilters[i].gameObject.SetActive(false);

        //     i++;
        // }
        // go.transform.GetComponent<MeshFilter>().mesh = new Mesh();
        // go.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        // go.transform.gameObject.SetActive(true);
    }


    public static void AdvanceCombine(GameObject go){

        // All our children (and us)
        // MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);
        MeshFilter[] filters = MeshCombiner.GetFilterRecursively(go).ToArray();
        Debug.Log("Total Filters = " + filters.Length);

        // All the meshes in our children (just a big list)
        List<Material> materials = new List<Material>();
        // MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(true); // <-- you can optimize this
        MeshRenderer[] renderers = GetRendererRecursively(go).ToArray();
        Debug.Log("Total Filters+renderer = " + filters.Length + " : " + renderers.Length);
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.transform == go.transform){
                continue;
            }
            Material[] localMats = renderer.sharedMaterials;
            foreach (Material localMat in localMats){
                // Debug.Log("Material used = " + localMat.color + " : " + localMat.shader + " : " + localMat.name);
                if (!materials.Contains (localMat)){
                    materials.Add (localMat);
                }
            }
        }

        // Each material will have a mesh for it.
        List<Mesh> submeshes = new List<Mesh>();
        foreach (Material material in materials){
            // Debug.Log("Material used = " + material.color + " : " + material.shader + " : " + material.name);
            // Make a combiner for each (sub)mesh that is mapped to the right material.
            List<CombineInstance> combiners = new List<CombineInstance> ();
            foreach (MeshFilter filter in filters){
                if (filter.transform == go.transform) {
                    continue;
                }
                // The filter doesn't know what materials are involved, get the renderer.
                MeshRenderer renderer = filter.GetComponent<MeshRenderer> ();  // <-- (Easy optimization is possible here, give it a try!)
                if (renderer == null)
                {
                    Debug.LogError (filter.name + " has no MeshRenderer");
                    continue;
                }

                // Let's see if their materials are the one we want right now.
                Material[] localMaterials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < localMaterials.Length; materialIndex++)
                {
                    if (localMaterials [materialIndex] != material){
                        continue;
                    }
                    // This submesh is the material we're looking for right now.
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = filter.sharedMesh;
                    ci.subMeshIndex = materialIndex;
                    ci.transform = Matrix4x4.identity;
                    combiners.Add (ci);
                    // filter.transform.gameObject.SetActive(false);
                }
            }
            // Flatten into a single mesh.
            Mesh mesh = new Mesh ();
            mesh.CombineMeshes (combiners.ToArray(), true);
            submeshes.Add (mesh);
        }

        // The final mesh: combine all the material-specific meshes as independent submeshes.
        List<CombineInstance> finalCombiners = new List<CombineInstance>();
        foreach (Mesh mesh in submeshes)
        {
            CombineInstance ci = new CombineInstance ();
            ci.mesh = mesh;
            ci.subMeshIndex = 0;
            ci.transform = Matrix4x4.identity;
            finalCombiners.Add (ci);
        }
        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes (finalCombiners.ToArray(), false);
        go.transform.GetComponent<MeshFilter>().sharedMesh = finalMesh;
        Debug.Log ("Final mesh has " + submeshes.Count + " materials.");

        // for(int a=0; a<go.transform.childCount; a++){
        //     go.transform.GetChild(a).gameObject.SetActive(false);
        // }

        StaticBatchingUtility.Combine(go);

        // List<GameObject> children = new List<GameObject>();

        // children = MeshCombiner.CombineRecursively(go, go, children);
        // StaticBatchingUtility.Combine(children.ToArray(), go);
    }

    static List<GameObject> CombineRecursively(GameObject go, GameObject root, List<GameObject> children){
        if(go == null){
            return null;
        }
        for(int i=0; i<go.transform.childCount; i++){
            MeshCombiner.CombineRecursively(go.transform.GetChild(i).gameObject, root, children);
        }
        
        // StaticBatchingUtility.Combine(go, root);
        children.Add(go);
        return children;
    }

    static List<MeshFilter> GetFilterRecursively(GameObject go){
        List<MeshFilter> filters = new List<MeshFilter>();
        Queue<GameObject> frontier = new Queue<GameObject>();
        MeshFilter[] childMesh;
        frontier.Enqueue(go);
        GameObject fgo;

        while(frontier.Count!=0){
            fgo = frontier.Dequeue();
            childMesh = fgo.GetComponentsInChildren<MeshFilter>(true);
            foreach(MeshFilter f in childMesh){
                filters.Add(f);
            }
            for(int i=0; i<fgo.transform.childCount; i++){
                // Debug.Log("Child = "  + fgo.transform.GetChild(i).gameObject.name);
                frontier.Enqueue(fgo.transform.GetChild(i).gameObject);
            }
            
        }
        return filters;

        // if(go.GetComponentsInChildren<MeshFilter>(true).Length == 0){
        //     return f;
        // }
        // // f.AddRange(new List<MeshFilter>(go.GetComponentsInChildren<MeshFilter>(true)));
        // for(int i=0; i<go.transform.childCount; i++){
        //     Debug.Log("Child = "  + go.transform.GetChild(i).gameObject.name);
        //     f.AddRange(MeshCombiner.GetFilterRecursively(go.transform.GetChild(i).gameObject, f));
        // }
        // return f;
    }

    static List<MeshRenderer> GetRendererRecursively(GameObject go){
        List<MeshRenderer> renderer = new List<MeshRenderer>();
        Queue<GameObject> frontier = new Queue<GameObject>();
        MeshRenderer[] childMesh;
        frontier.Enqueue(go);
        GameObject fgo;

        while(frontier.Count!=0){
            fgo = frontier.Dequeue();
            childMesh = fgo.GetComponentsInChildren<MeshRenderer>(true);
            foreach(MeshRenderer f in childMesh){
                renderer.Add(f);
            }
            for(int i=0; i<fgo.transform.childCount; i++){
                // Debug.Log("Child = "  + fgo.transform.GetChild(i).gameObject.name);
                frontier.Enqueue(fgo.transform.GetChild(i).gameObject);
            }
            
        }
        return renderer;
    }

    public static void CombineMeshesNew(GameObject go){
        Vector3 basePosition = go.transform.position;
        Quaternion baseRotation = go.transform.rotation;
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        
        ArrayList materials = new ArrayList();
        ArrayList combineInstanceArrays = new ArrayList();
        MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

            if (!meshRenderer ||
                !meshFilter.sharedMesh ||
                meshRenderer.sharedMaterials.Length != meshFilter.sharedMesh.subMeshCount)
                {
                    continue;
                }

            for (int s = 0; s < meshFilter.sharedMesh.subMeshCount; s++)
            {
                int materialArrayIndex = Contains(materials, meshRenderer.sharedMaterials[s].name);
                if (materialArrayIndex == -1)
                {
                    materials.Add(meshRenderer.sharedMaterials[s]);
                    materialArrayIndex = materials.Count - 1;
                }
                combineInstanceArrays.Add(new ArrayList());

                CombineInstance combineInstance = new CombineInstance();
                combineInstance.transform = meshRenderer.transform.localToWorldMatrix;
                combineInstance.subMeshIndex = s;
                combineInstance.mesh = meshFilter.sharedMesh;
                (combineInstanceArrays[materialArrayIndex] as ArrayList).Add(combineInstance);
            }
        }

        // Get / Create mesh filter & renderer
        MeshFilter meshFilterCombine = go.GetComponent<MeshFilter>();
        if (meshFilterCombine == null)
        {
            meshFilterCombine = go.AddComponent<MeshFilter>();
        }
        MeshRenderer meshRendererCombine = go.GetComponent<MeshRenderer>();
        if (meshRendererCombine == null)
        {
            meshRendererCombine = go.AddComponent<MeshRenderer>();
        }

        // Combine by material index into per-material meshes
        // also, Create CombineInstance array for next step
        Mesh[] meshes = new Mesh[materials.Count];
        CombineInstance[] combineInstances = new CombineInstance[materials.Count];

        for (int m = 0; m < materials.Count; m++)
        {
            CombineInstance[] combineInstanceArray = (combineInstanceArrays[m] as ArrayList).ToArray(typeof(CombineInstance)) as CombineInstance[];
            meshes[m] = new Mesh();
            meshes[m].CombineMeshes(combineInstanceArray, true, true);

            combineInstances[m] = new CombineInstance();
            combineInstances[m].mesh = meshes[m];
            combineInstances[m].subMeshIndex = 0;
        }

        // Combine into one
        meshFilterCombine.sharedMesh = new Mesh();
        meshFilterCombine.sharedMesh.CombineMeshes(combineInstances, false, false);

        // Destroy other meshes
        foreach (Mesh oldMesh in meshes)
        {
            oldMesh.Clear();
            // DestroyImmediate(oldMesh);
        }

        // Assign materials
        Material[] materialsArray = materials.ToArray(typeof(Material)) as Material[];
        meshRendererCombine.materials = materialsArray;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            // DestroyImmediate(meshFilter.gameObject);
        }

        go.transform.position = basePosition;
        go.transform.rotation = baseRotation;
    }


    private static int Contains(ArrayList searchList, string searchName)
    {
        for (int i = 0; i < searchList.Count; i++)
        {
            if (((Material)searchList[i]).name == searchName)
            {
                return i;
            }
        }
        return -1;
    }
}

