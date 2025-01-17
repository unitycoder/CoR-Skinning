﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoR
{
    //[ExecuteInEditMode]
    public class SkinnedCor : MonoBehaviour
    {
        public CorAsset corAsset;
        public Mesh optionalHdMesh;
        public Texture2D weightTexture;
        //public Shader test = Shader.Find
        BaseCorSkinning skinning;
  
        // only keeping values for switching modes
        Mesh modifyMesh;
        Transform[] bones;
        Material[] materials;
        int vertexCount;
        //Material mat;
        bool initialized = false;
        bool registered = false;

        public bool gpuEnabled {
            get
            {
                return SystemInfo.supportsComputeShaders;
            }
        }

        private void Awake()
        {
            if (!initialized)
            {
                initializeSkinning();
            }
        }
        private void OnEnable()
        {
            if (!initialized)
            {
                initializeSkinning();
                return;
            }
            registerInstace();
        }
        void OnDestroy()
        {
            if (skinning != null)
            {
                skinning.Destroy();
                skinning = null;
            }
        }
        private void OnDisable()
        {
            if (registered)
            {
                deregisterInstance();
            }
        }
        void registerInstace()
        {
            if (!registered)
            {
                registered = true;
                //CoRManager.instance.instances.Add(skinning);
                if (!CoRManager.instance.sortedInstances.ContainsKey(corAsset))
                {
                    CoRManager.instance.addInstanceType(corAsset);
                }
                CoRManager.instance.sortedInstances[corAsset].instances.Add(skinning);
            }
        }
        void deregisterInstance()
        {
            if (registered)
            {
                registered = false;
                //CoRManager.instance.instances.Remove(skinning);
                CoRManager.instance.sortedInstances[corAsset].instances.Remove(skinning);
                if (CoRManager.instance.sortedInstances[corAsset].instances.Count == 0)
                {
                    CoRManager.instance.removeInstanceType(corAsset);
                }
            }
        }
        private void initializeSkinning()
        {
            if (CoRManager.instance == null)
            {
                CoRManager.instance = new GameObject("CoR Manager").AddComponent<CoRManager>();
            }
            if (!enabled)
            {
                return;
            }
            if (corAsset == null)
            {
                throw new System.Exception("CoRAsset required. Click 'Create CoR Asset'");
            }
            if (corAsset.pStar.Length == 0)
            {
                throw new System.Exception("Need to pre process core asset. ");
            }
            var skin = GetComponent<SkinnedMeshRenderer>();
            if (skin == null)
            {
                throw new System.Exception("SkinnedMeshRenderer required");
            }

            // doesn't seem to animate the bones if it doesn't find a skinned mesh
            // TODO: find a better way to handle this
            var anim = GetComponentInParent<Animator>();
            if (anim != null)
            {
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            List<Transform> usedBones = new List<Transform>();
            for (int i = 0; i < corAsset.usedBones.Length; i++)
            {
                usedBones.Add(skin.bones[corAsset.usedBones[i]]);
            }
            //bones = skin.bones;
            bones = usedBones.ToArray();
            var mf = gameObject.AddComponent<MeshFilter>();
            vertexCount = skin.sharedMesh.vertexCount;
            //mat = skin.materials[0];
            modifyMesh = (Mesh)GameObject.Instantiate(skin.sharedMesh); // clone
            //modifyMesh.MarkDynamic(); // Optimize mesh for frequent updates.
            mf.mesh = modifyMesh;
            modifyMesh.RecalculateBounds();

            materials = skin.materials;

            var meshRend = gameObject.AddComponent<MeshRenderer>();
            meshRend.shadowCastingMode = skin.shadowCastingMode;
            meshRend.allowOcclusionWhenDynamic = false;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].SetFloat(Shader.PropertyToID("_Initialized"), 1);
                //materials[i] = mat;
            }
            meshRend.sharedMaterials = materials;

            skin.enabled = false;
            Destroy(skin);

            ChangeSkinning();
            initialized = true;

            registerInstace();
        }
       
        private void ChangeSkinning()
        {
            if (skinning != null)
            {
                skinning.Destroy();
                skinning = null;
            }

            bool cpu = true;
            if (gpuEnabled)
            {
                skinning = new CorGPUSkinning();
                cpu = false;
            }
            else
            {
                skinning = new CorCPUSkinning();
            }

            skinning.Setup(corAsset, bones, gameObject, modifyMesh, materials, cpu);
        }

        // FixedUpdate(), LateUpdate() or  Update(). Using FixedUpdate() for testing 
        //void LateUpdate()
        //{
        //    skinning.Skin(corAsset.globalCorWeight);

        //    //return;
        //    //Graphics.DrawProcedural(
        //    //mat,
        //    //new Bounds(transform.position, transform.lossyScale * 5),
        //    //MeshTopology.Triangles, vertexCount, 1,
        //    //null, null,
        //    //ShadowCastingMode.Off, true, gameObject.layer);
        //}
    }

}