using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class InspectHV { public static string Main(){var l=Object.FindFirstObjectByType<ElevatorLesson>();var pause=l.pauseMenu;var rs=pause.canvas.GetComponentsInChildren<RectTransform>(true).Select(r=>new{name=r.name,pos=r.position.ToString(),local=r.localPosition.ToString(),scale=r.lossyScale.ToString(),size=r.rect.size.ToString(),pivot=r.pivot.ToString(),anchors=r.anchorMin.ToString()+r.anchorMax.ToString()}).ToArray();return Newtonsoft.Json.JsonConvert.SerializeObject(new{path=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,Application.isPlaying,pause=rs,observations=l.CollectObservations().Where(o=>o.gameObject.activeInHierarchy).Select(o=>new{c=o.clue.ToString(),n=o.name,p=o.transform.position.ToString(),bounds=o.GetComponent<Collider>()?.bounds.ToString()}).ToArray()});}}