using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CatTang.EditorTools
{
    static class MirrorBladeMesh
    {
        const string Model = "Assets/Art/Characters/Ch3/Ch3_Blade.fbx";
        const string Prefab = "Assets/Resources/Enemies/Ch3_Blade.prefab";
        const string Output = "Assets/Art/Characters/Ch3/Ch3_BladeLongMesh.asset";

        // The blade is skinned into the same mesh as the body. Keep its vertices
        // and weights, and stretch only the blade beyond the crossguard in bind pose.
        [MenuItem("CatTang/거울 검객 칼날 길이 적용")]
        public static void Apply()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Model).GetComponentInChildren<SkinnedMeshRenderer>();
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var target = root.GetComponentInChildren<SkinnedMeshRenderer>();
                var input = source.sharedMesh;
                var vertices = input.vertices;
                var weights = input.boneWeights;
                var bind = input.bindposes;
                var bones = source.bones;
                int wrist = Find(bones, "RightHand"), forearm = Find(bones, "RightForeArm"), thumb = Find(bones, "RightHandThumb1");
                var wristPos = Position(bind[wrist]);
                var arm = wristPos - Position(bind[forearm]);
                float unit = arm.magnitude;
                arm /= unit;
                var side = Vector3.ProjectOnPlane(Position(bind[thumb]) - wristPos, arm).normalized;
                var normal = Vector3.Cross(arm, side);
                var grip = ToMeshPoint(Enemy.MirrorBladeGrip);
                var tip = ToMeshPoint(Enemy.MirrorBladeTip);
                var axis = (tip - grip).normalized;
                float maxBlade = Vector3.Distance(grip, tip) + unit * 0.18f;
                var handBone = new bool[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                {
                    string name = bones[i].name;
                    handBone[i] = name.Contains("RightHand") || name.EndsWith("RightForeArm", StringComparison.Ordinal);
                }

                int moved = 0;
                float edge = 0f, newEdge = 0f;
                var normals = input.normals;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var w = weights[i];
                    float weight = (handBone[w.boneIndex0] ? w.weight0 : 0f) + (handBone[w.boneIndex1] ? w.weight1 : 0f)
                                 + (handBone[w.boneIndex2] ? w.weight2 : 0f) + (handBone[w.boneIndex3] ? w.weight3 : 0f);
                    if (weight < 0.60f) continue;
                    var fromGrip = vertices[i] - grip;
                    float along = Vector3.Dot(fromGrip, axis);
                    var width = fromGrip - along * axis;
                    if (along <= unit * 0.16f || along > maxBlade || width.sqrMagnitude > unit * unit * 0.59f * 0.59f) continue;

                    float blend = Mathf.SmoothStep(0f, 1f, (along / unit - 0.16f) / 0.40f);
                    float lengthScale = Mathf.Lerp(1f, Enemy.MirrorBladeLengthScale, blend);
                    float widthScale = Mathf.Lerp(1f, 1.11f, blend);
                    vertices[i] = grip + along * lengthScale * axis + width * widthScale;
                    if (normals.Length == vertices.Length)
                    {
                        var axialNormal = Vector3.Project(normals[i], axis);
                        normals[i] = (axialNormal / lengthScale + (normals[i] - axialNormal) / widthScale).normalized;
                    }
                    moved++;
                    edge = Mathf.Max(edge, along);
                    newEdge = Mathf.Max(newEdge, along * lengthScale);
                }
                if (moved < 50 || moved > vertices.Length * 0.10f)
                    throw new InvalidOperationException($"Mirror blade mask selected {moved} of {vertices.Length} vertices.");

                var mesh = Object.Instantiate(input);
                mesh.name = Enemy.MirrorBladeLongMeshName;
                mesh.vertices = vertices;
                if (normals.Length == vertices.Length) mesh.normals = normals;
                mesh.RecalculateBounds();
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(Output);
                if (saved)
                {
                    EditorUtility.CopySerialized(mesh, saved);
                    Object.DestroyImmediate(mesh);
                }
                else
                {
                    AssetDatabase.CreateAsset(mesh, Output);
                    saved = mesh;
                }
                target.sharedMesh = saved;
                var coverage = source.localBounds;
                coverage.Encapsulate(saved.bounds);
                // A longer tip also travels farther while skinned to the wrist.
                coverage.Expand(2f * (newEdge - edge));
                target.localBounds = coverage;
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                AssetDatabase.SaveAssets();
                Debug.Log($"Mirror blade resized: {moved}/{vertices.Length} vertices; cutting edge {newEdge / edge:P0} of original.");

                Vector3 ToMeshPoint(Vector3 point) => wristPos + unit * (arm * point.x + side * point.y + normal * point.z);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static int Find(Transform[] bones, string suffix)
        {
            int index = Array.FindIndex(bones, bone => bone && bone.name.EndsWith(suffix, StringComparison.Ordinal));
            if (index < 0) throw new InvalidOperationException($"Mirror blade bone '{suffix}' is missing.");
            return index;
        }

        static Vector3 Position(Matrix4x4 bind) => bind.inverse.GetColumn(3);
    }
}

