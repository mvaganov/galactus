﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AgentSoul : MonoBehaviour {
//    public AgentSensor sensor;
//
//    public Group team;
//
//    public float cameraDistance = 3;
//    public Transform cameraTransform;
    private bool //isPosessing = false, 
	needsBody = true;
//    //private PlayerForce posessed = null;
//
//	private List<AgentForce> posessedBodies = new List<AgentForce>();
//
//    private Vector2 move;
//    public float xSensitivity = 5, ySensitivity = 5;
//    public bool invertY = false;
//    public bool holdVector = false;
//
//    void Start()
//    {
//        if(team == null)
//        {
//            // TODO make some kind of mechanism that allows a player to pick an existing team instead of creating a new one for themselves
//            // TODO make a mechanism that has the team allwo or deny entry for new members, including rules for officers, majority vs consensus, weighted votes, and possibly ranked voting
//			team = Singleton.Get<GroupManager>().NewGroup(name, this);
//        }
//    }
//
//    void Update() {
//        //if(posessed && posessed.IsAlive()) {
//        //    posessed.GetResourceEater().DoUserActions(cameraTransform);
//        //} else 
//        bool processed = false;
//        if (!processed && posessedBodies.Count > 0) {
//            if (posessedBodies.Count == 1) {
//                posessedBodies[0].DoUserActions(cameraTransform);
//            } else {
//                // create a copy of the list of bodies in case the list is modified in the update of one of the bodies
//				List<AgentForce> bodies = new List<AgentForce>(posessedBodies);
//				foreach (AgentForce pf in bodies) {
//                    processed |= pf.DoUserActions(cameraTransform);
//                }
//            }
//        }
//    }
//
//    public Transform GetLookTransform() { return cameraTransform; }
//
//    void LateUpdate()
//    {
//        move = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
//        if (move.x != 0)
//        {
//            transform.Rotate(0, move.x * xSensitivity, 0);
//        }
//        if (move.y != 0)
//        {
//            if (invertY) { move.y *= -1; }
//            transform.Rotate(-move.y * ySensitivity, 0, 0);
//        }
////        if (//posessed || 
////            posessedBodies.Count > 0) {
////            var d = Input.GetAxis("Mouse ScrollWheel");
////            if (d > 0f) { cameraDistance -= 0.125f; if (cameraDistance < 0) cameraDistance = 0; }
////            else if (d < 0f) { cameraDistance += 0.125f; }
////            Vector3 center = Vector3.zero;
////            float scale = 0;
////            //if (posessed) {
////            //    center = posessed.transform.position;
////            //    scale = posessed.transform.lossyScale.z;
////            //} else
////            {
////                //foreach(PlayerForce pf in posessedBodies) {
////                //    center += pf.transform.position;
////                //    scale += pf.transform.lossyScale.z;
////                //}
////                //center /= posessedBodies.Count;
////                //scale /= posessedBodies.Count;
////                ResourceEater re = GetBiggestBody();
////                center = re.transform.position;
////                scale = re.transform.lossyScale.z;
////            }
////            Vector3 delta = cameraTransform.forward.normalized * cameraDistance * scale;
////            transform.position = center - delta;
////        }
//        holdVector = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
//    }
//
//
//    [System.Serializable]
//    public class Settings
//    {
//        public string name = "you";
//        // TODO add player preferences for: color, invertY, sensitivityX, sensitivityY
//    }
//    public Settings settings = new Settings();
//
//
//    public bool IsPosessing() { return isPosessing; }
//	public bool IsDisembodied() {
//        return (//posessed == null || 
//            posessedBodies.Count == 0) && !isPosessing; }
    public bool IsInNeedOfBody() { return needsBody; }
//    public void SetNeedsBody(bool need) { this.needsBody = need; }
//
//	public AgentForce GetBiggestBody() {
//        if (posessedBodies.Count == 0) return null;
//		AgentForce biggest = posessedBodies[0];
//		AgentForce e;
//        for (int i = 1; i < posessedBodies.Count; ++i) {
//            e = posessedBodies[i];
//            if (e.GetMass() > biggest.GetMass()) {
//                biggest = e;
//            }
//        }
//        sensor.sensorOwner = biggest;
//        return biggest;
//    }
//
//    public void Disconnect(AgentForce pf) {
//        bool somethingDisconnected = false;
//        //if (posessed == pf) {
//        //    Prediction pred = GetComponent<Prediction> ();
//		//	pred.toPredict = null;
//        //    posessed = null;
//        //    somethingDisconnected = true;
//        //    sensor.sensorOwner = null;
//        //}
//        if (posessedBodies.Count > 0) {
//            somethingDisconnected = posessedBodies.Remove(pf);
//            Prediction pred = GetComponent<Prediction>();
//            AgentForce primary = GetBiggestBody();
//            sensor.sensorOwner = primary;
//            //pred.toPredict = primary?primary.GetPlayerForce().transform:null;
//            pred.SetBodies(null);
//        }
//        if (somethingDisconnected) {
//            pf.SetUserSoul(null);
//        } else {
//            Debug.LogError("invalid component: Attempted to remove " + pf+" from "+this);
//        }
//        isPosessing = false;
//    }
//
//    public AgentForce GetPossesed() {
//        return GetBiggestBody();//posessed; 
//        }
//
    public void Posess(AgentForce pf, bool asOnlyBody) {
//        if (asOnlyBody) {
//            // remove all other bodies that are posessed
//            //if(posessed) Disconnect(posessed);
//            //else
//            if (posessedBodies.Count > 0) {
//                for(int i = posessedBodies.Count-1; i >= 0; ++i) {
//                    Disconnect(posessedBodies[i]);
//                }
//            }
//        }
//        isPosessing = true;
//        Transform n = pf.transform;
//        SetNeedsBody(false);
//        Vector3 oposition = transform.position;
//        Vector3 oscale = transform.localScale;
//        Quaternion orotation = transform.rotation;
//        n.gameObject.SetActive(false); // hide the player until the zeroing-in-on-body process finishes
//        TimeMS.LambdaProgress zeroingInOnNextBody = (t) => {
//            transform.position = Vector3.Lerp(oposition, n.position, t);
//            transform.rotation = Quaternion.Lerp(orotation, n.rotation, t);
//            transform.localScale = Vector3.Lerp(oscale, n.lossyScale, t);
//            if (t >= 1)
//            {
//                pf.gameObject.SetActive(true);
//                //posessed = pf;
//                posessedBodies.Add(pf);
//                AgentPrediction pred = GetComponent<AgentPrediction>();
//                //pred.toPredict = n;
//                pred.SetBodies(posessedBodies);
//                AgentForce re = pf;
//                re.name = settings.name;
//                re.SetTeam(team);
//                pf.SetUserSoul(this);
//                transform.localScale = new Vector3(1, 1, 1);
//                isPosessing = false;
//                sensor.RefreshSensorOwner(re);
//                //Debug.Log("watching for " + name + "'s destruction");
//                // remove the soul before destruction...
//                MemoryPoolRelease.Add(pf.gameObject, (obj) => {
//                    //Debug.Log("I'm " + name + " and I'm Dead!");
//                    if(//posessed == pf || 
//                        posessedBodies.Contains(pf)) Disconnect(pf);
//                });
//
//            }
//        };
//        TimeMS.CallbackWithDuration(1000, zeroingInOnNextBody);
    }
}
