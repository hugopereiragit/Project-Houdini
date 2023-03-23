using System.Net;
using System.Security.AccessControl;
//================================================================================================================================
//
//  Copyright (c) 2015-2022 VisionStar Information Technology (Shanghai) Co., Ltd. All Rights Reserved.
//  EasyAR is the registered trademark or trademark of VisionStar Information Technology (Shanghai) Co., Ltd in China
//  and other countries for the augmented reality technology developed by VisionStar Information Technology (Shanghai) Co., Ltd.
//
//================================================================================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using Firebase;
using Firebase.Extensions;
using Firebase.Storage;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SpatialMap_SparseSpatialMap
{
    public class MainViewController : MonoBehaviour
    {
        public Button Edit;
        public Button Preview;
        public Button Create;
        public GameObject ClearPupup;
        public MapGridController MapGrid;

      //Firebase variables 
        FirebaseStorage storage;
        StorageReference storageReference;


        class ListFilesResponse
        {
            public List<FileMetadata> items { get; set; }
        }

        // Class that represents metadata for a file in Firebase Storage
        class FileMetadata
        {
            public string name { get; set; }
            public string bucket { get; set; }
        }



         //get the files list from firebase storage
        async Task<List<FileMetadata>> GetFilesInFolder(string path)
            {
                const string baseUrl = "https://firebasestorage.googleapis.com/v0/b/";
                const string projectId = "houdini-ac884.appspot.com";
                string url = $"{baseUrl}{projectId}/o?prefix={path}";
                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(url))
                using (HttpContent content = response.Content)
                {   
                    Debug.Log("Content");
                    Debug.Log(content);

                    string responseText = await content.ReadAsStringAsync();
                    Debug.Log("Response Text");
                    Debug.Log(responseText); 
                    
                    ListFilesResponse responseData = JsonConvert.DeserializeObject<ListFilesResponse>(responseText);

                    // Return the list of files
                    Debug.Log("Response Data");
                    Debug.Log(responseData.items);
                    Debug.Log((responseData.items).ToString());
                    return responseData.items;

                    
                }
            }

        private async void OnEnable()
        { 
            StopAllCoroutines();

            var colors = Create.colors;
            colors.normalColor = new Color(0.2f, 0.58f, 0.988f);
            colors.highlightedColor = new Color(0.192f, 0.557f, 0.949f);
            colors.pressedColor = new Color(0.157f, 0.455f, 0.773f);
            Create.colors = colors;
            ClearPupup.SetActive(false);
            StartCoroutine(Twinkle(Create));
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            easyar.GUIPopup.EnqueueMessage("AQUI", 5);
      


// ------------------------- FILE DOWNLOADING ------------------------------------- //
            Debug.Log("Download Attempt...");
              Debug.Log("STEP11131...");

            string folderPath = "uploads/";
            //GetFilesInFolder(folderPath);
            List<FileMetadata> files = await GetFilesInFolder(folderPath);
            foreach (FileMetadata file in files)
                {
                    // Get the file's download URL
                    string name = file.name;
                    string bucket = file.bucket;
                     Debug.Log("File name");
                     Debug.Log(name);
                
                
                    // Check for permissions
                    if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite)){
                        Debug.Log("STEP1...");
                        //Firestore Reference
                        storage = FirebaseStorage.DefaultInstance;
                        storageReference = storage.GetReferenceFromUrl("gs://houdini-ac884.appspot.com");
                        StorageReference riversRef = storageReference.Child(name.ToString());
                        // Hardcode Version //storageReference pathReference =storage.GetReference("uploads/3895d968-65bf-4e2d-a964-763e22742fdf.meta");
                        // Create local filesystem URL
                        Debug.Log("STEP2...");
                        //removing upload from file name
                        string toRemove = "uploads/";
                        string result = string.Empty;
                        int i = name.IndexOf(toRemove);
                        if (i >= 0)
                        {   
                             Debug.Log("STEP2.1..");
                            result= name.Remove(i, toRemove.Length);
                        }
                        Debug.Log("STEP2.2.."); // Get the directory to store the file on
                        var Directory_path = ("SparseSpatialMap/" + result);
                        var path = (Application.persistentDataPath + "/" + Directory_path);   
                        Debug.Log("STEP3...");
                            riversRef.GetFileAsync(path).ContinueWithOnMainThread(task => {
                            if (!task.IsFaulted && !task.IsCanceled) {
                                
                                Debug.Log("Finished downloading...");
                                
                            easyar.GUIPopup.EnqueueMessage("Download Completed", 5);
                            }else{
                                
                                Debug.Log("DOWNLOAD FAILURE !!!!!!!!!!!!!");
                                Debug.Log(task.Exception.ToString());
    
                            easyar.GUIPopup.EnqueueMessage("FAIL EXCEPTION", 5);
                            }
                        Debug.Log("STEP4...");
                        });

                        } 
                        else { // If no permissions request permissions
                            Debug.Log("No Permissions");
                            easyar.GUIPopup.EnqueueMessage("FAIL, No permissions", 5);
                            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
                            }

                        Debug.Log("End of Download Attempt...");
                } 
        }
        
// ------------------------- FILE DOWNLOADING END ------------------------------------- //

        public void EnableEdit(bool enable)
        {
            Edit.interactable = enable;
        }

        public void EnablePreview(bool enable)
        {
            Preview.interactable = enable;
        }

        private IEnumerator Twinkle(Button button)
        {
            if (MapGrid.CellCount > 0) { yield break; }

            var colors = button.colors;
            var olist = new List<Color>
            {
                colors.normalColor,
                colors.highlightedColor,
                colors.pressedColor
            };

            var clist = new List<Vector3>();
            foreach (var color in olist)
            {
                Vector3 hsv;
                Color.RGBToHSV(color, out hsv.x, out hsv.y, out hsv.z);
                clist.Add(hsv);
            }

            float smin = 0.2f;
            float smax = clist[0].y;
            bool increase = false;

            while (MapGrid.CellCount <= 0)
            {
                for (int i = 0; i < clist.Count; ++i)
                {
                    var hsv = clist[i];
                    hsv.y += increase ? 0.2f * Time.deltaTime : -0.2f * Time.deltaTime;
                    clist[i] = hsv;
                }
                if (clist[0].y >= smax)
                {
                    for (int i = 0; i < clist.Count; ++i)
                    {
                        clist[i] = new Vector3(clist[i].x, smax, clist[i].z);
                    }
                    increase = false;
                }
                else if (clist[0].y < smin)
                {
                    for (int i = 0; i < clist.Count; ++i)
                    {
                        clist[i] = new Vector3(clist[i].x, smin, clist[i].z);
                    }
                    increase = true;
                }
                colors.normalColor = Color.HSVToRGB(clist[0].x, clist[0].y, clist[0].z);
                colors.highlightedColor = Color.HSVToRGB(clist[1].x, clist[1].y, clist[1].z);
                colors.pressedColor = Color.HSVToRGB(clist[2].x, clist[2].y, clist[2].z);
                button.colors = colors;
                yield return 0;
            }
            colors.normalColor = olist[0];
            colors.highlightedColor = olist[1];
            colors.pressedColor = olist[2];
            button.colors = colors;

        }
    }

}
