//================================================================================================================================
//
//  Copyright (c) 2015-2022 VisionStar Information Technology (Shanghai) Co., Ltd. All Rights Reserved.
//  EasyAR is the registered trademark or trademark of VisionStar Information Technology (Shanghai) Co., Ltd in China
//  and other countries for the augmented reality technology developed by VisionStar Information Technology (Shanghai) Co., Ltd.
//
//================================================================================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using System;
using System.IO;
using System.Threading;
using Firebase;
using Firebase.Extensions;
using Firebase.Storage;

namespace SpatialMap_SparseSpatialMap
{
    public class EditViewController : MonoBehaviour
    {

        FirebaseStorage storage;
        StorageReference storageReference;

        public Dragger PropDragger;
        public GameObject Tips;

        private MapSession.MapData mapData;
        private bool isTipsOn;

        private void Awake()
        {
            PropDragger.CreateObject += (gameObj) =>
            {
                if (gameObj)
                {
                    gameObj.transform.parent = mapData.Controller.transform;
                    mapData.Props.Add(gameObj);
                }
            };
            PropDragger.DeleteObject += (gameObj) =>
            {
                if (gameObj)
                {
                    mapData.Props.Remove(gameObj);
                }
            };
        }

        private void OnEnable()
        {
            Tips.SetActive(false);
            isTipsOn = false;
        }

        public void SetMapSession(MapSession session)
        {
            mapData = session.Maps[0];
            PropDragger.SetMapSession(session);
        }

        public void ShowTips()
        {
            isTipsOn = !isTipsOn;
            Tips.SetActive(isTipsOn);
        }

        public void Save()
        {
            Debug.Log("222");
            Debug.Log(Application.persistentDataPath);
            if (mapData == null)
            {
                Debug.Log("111");
                return;
            }

            var propInfos = new List<MapMeta.PropInfo>();

            foreach (var prop in mapData.Props)
            {
                var position = prop.transform.localPosition;
                var rotation = prop.transform.localRotation;
                var scale = prop.transform.localScale;

                propInfos.Add(new MapMeta.PropInfo()
                {
                    Name = prop.name,
                    Position = new float[3] { position.x, position.y, position.z },
                    Rotation = new float[4] { rotation.x, rotation.y, rotation.z, rotation.w },
                    Scale = new float[3] { scale.x, scale.y, scale.z }
                });
            }
            mapData.Meta.Props = propInfos;



            MapMetaManager.Save(mapData.Meta);


// ------------------------- FILE UPLOADING ------------------------------------- //

             if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead)){ //Empty white files fixed by closing file streams
                    Debug.Log("Permissions Found");
                    DirectoryInfo d = new DirectoryInfo(Application.persistentDataPath + "/SparseSpatialMap");
                    FileInfo[] Files = d.GetFiles("*.meta"); //Getting meta files
                    string str = "";
                    foreach(FileInfo file in Files )
                        {
                    str = str + ", " + file.Name;
                    var Directory_path = ("SparseSpatialMap/" + file.Name);
                    var path = (Application.persistentDataPath + "/" + Directory_path);             
                    //Firestore Reference
                    storage = FirebaseStorage.DefaultInstance;
                    storageReference = storage.GetReferenceFromUrl("gs://houdini-ac884.appspot.com");
                    // File located on disk
                    string localFile = path.ToString();
                    StreamReader stream = new StreamReader(path);
                    // Create a reference to the file you want to upload
                    StorageReference riversRef = storageReference.Child("uploads/"+ file.Name);
                    // Upload the file to the path "uploads/newFile.meta"
                    riversRef.PutStreamAsync(stream.BaseStream)
                        .ContinueWith((task) => {
                            if (task.IsFaulted || task.IsCanceled) {
                                Debug.Log(task.Exception.ToString());
                                // Uh-oh, an error occurred!
                                stream.Close();
                            }
                            else {
                                // Metadata contains file metadata such as size, content-type, and download URL.
                                StorageMetadata metadata = task.Result;
                                string md5Hash = metadata.Md5Hash;
                                Debug.Log("Finished uploading...");
                                Debug.Log("md5 hash = " + md5Hash);
                                stream.Close();
                            }
                        });
                        }

                        Debug.Log(str);
 
                        } else {
                                Debug.Log("No Permissions");
                                Permission.RequestUserPermission(Permission.ExternalStorageRead);
                                return;
                        }
            
// ------------------------- FILE UPLOADING END ------------------------------------- //
            Debug.Log("fim");

        }


        public void Delete(){
            Debug.Log("222");
            Debug.Log(Application.persistentDataPath);
            if (mapData == null)
            {
                Debug.Log("111");
                return;
            }
            Debug.Log("Beginning of map data");
            //Debug.Log(mapData.MapName);
            //Debug.Log(mapData.MapName.ToString());
            Debug.Log(mapData.Meta.Map.ID);
            //Debug.Log(mapData.Meta.Map);
            //Debug.Log(mapData.Meta.Map.Name);
            //Debug.Log(mapData.Meta);
            //Debug.Log(mapData.Meta.name);
            //Debug.Log(name);
            //Debug.Log(Session.Name);
            //Debug.Log(session);
            Debug.Log("Ending of map data");


            //Firestore Reference
            storage = FirebaseStorage.DefaultInstance;
            storageReference = storage.GetReferenceFromUrl("gs://houdini-ac884.appspot.com");
            // Create a reference to the file to delete.
            StorageReference desertRef = storageReference.Child("uploads/" + mapData.Meta.Map.ID + ".meta");
            // Delete the file
            desertRef.DeleteAsync().ContinueWithOnMainThread(task => {
                if (task.IsCompleted) {
                    //deleting from the firebase complete now delete from the internal storage
                    var Directory_path = ("SparseSpatialMap/" + mapData.Meta.Map.ID + ".meta");
                    var path = (Application.persistentDataPath + "/" + Directory_path);  
                    File.Delete(path);
                    Debug.Log("File deleted successfully.");
                    return;
                }
                else {
                    // Uh-oh, an error occurred!
                     Debug.Log("File deleted unsuccessfully.");
                     return;
                }
            });


        }
    }
}
