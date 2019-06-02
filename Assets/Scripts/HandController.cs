using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kai.SDK;
using WebSocketSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class HandController : MonoBehaviour
{
	public Animation leftIndexFingerAnimation;
	public Animation leftMiddleFingerAnimation;
	public Animation leftRingFingerAnimation;
	public Animation leftLittleFingerAnimation;
    
	public Animation rightIndexFingerAnimation;
	public Animation rightMiddleFingerAnimation;
	public Animation rightRingFingerAnimation;
	public Animation rightLittleFingerAnimation;

    public Transform leftOrientationObject;
    public Transform rightOrientationObject;

    public GameObject bullet;
    public float bulletSpeed = 1f;

    public float shootTimeout = 0.25f;

    public float animationSpeed = 5f;
	
	private bool leftIndexFingerState = false;
    private bool leftIndexFinger = false;
	private bool leftMiddleFingerState = false;
    private bool leftMiddleFinger = false;
	private bool leftRingFingerState = false;
    private bool leftRingFinger = false;
	private bool leftLittleFingerState = false;
    private bool leftLittleFinger = false;

    
	
	private bool rightIndexFingerState = false;
    private bool rightIndexFinger = false;
	private bool rightMiddleFingerState = false;
    private bool rightMiddleFinger = false;
	private bool rightRingFingerState = false;
    private bool rightRingFinger = false;
	private bool rightLittleFingerState = false;
    private bool rightLittleFinger = false;

    private Quaternion leftKaiQuat = new Quaternion();
    private Quaternion rightKaiQuat = new Quaternion();

    private float currentTimeout;

    void Start()
    {
        currentTimeout = shootTimeout;
        KaiSDK.Initialise("test", "qwerty");

        KaiSDK.DefaultLeftKai.SetCapabilities(KaiCapabilities.FingerShortcutData | KaiCapabilities.QuaternionData);
        KaiSDK.DefaultRightKai.SetCapabilities(KaiCapabilities.FingerShortcutData | KaiCapabilities.QuaternionData);

        KaiSDK.DefaultLeftKai.FingerShortcut += OnLeftFingerShortcutData;
        KaiSDK.DefaultLeftKai.QuaternionData += OnLeftQuaternionData;
        
        KaiSDK.DefaultRightKai.FingerShortcut += OnRightFingerShortcutData;
        KaiSDK.DefaultRightKai.QuaternionData += OnRightQuaternionData;

        KaiSDK.Connect();
    }

    // Update is called once per frame
    void Update()
    {
        if(leftIndexFinger != leftIndexFingerState)
        {
            leftIndexFingerState = leftIndexFinger;
            foreach (AnimationState state in leftIndexFingerAnimation)
            {
                state.speed = leftIndexFinger ? animationSpeed : -animationSpeed;
                state.time = leftIndexFinger ? 0 : state.length;
            }
            leftIndexFingerAnimation.Play();
        }
        if(leftMiddleFinger != leftMiddleFingerState)
        {
            leftMiddleFingerState = leftMiddleFinger;
            foreach (AnimationState state in leftMiddleFingerAnimation)
            {
                state.speed = leftMiddleFinger ? animationSpeed : -animationSpeed;
                state.time = leftMiddleFinger ? 0 : state.length;
            }
            leftMiddleFingerAnimation.Play();
        }
        if(leftRingFinger != leftRingFingerState)
        {
            leftRingFingerState = leftRingFinger;
            foreach (AnimationState state in leftRingFingerAnimation)
            {
                state.speed = leftRingFinger ? animationSpeed : -animationSpeed;
                state.time = leftRingFinger ? 0 : state.length;
            }
            leftRingFingerAnimation.Play();
        }
        if(leftLittleFinger != leftLittleFingerState)
        {
            leftLittleFingerState = leftLittleFinger;
            foreach (AnimationState state in leftLittleFingerAnimation)
            {
                state.speed = leftLittleFinger ? animationSpeed : -animationSpeed;
                state.time = leftLittleFinger ? 0 : state.length;
            }
            leftLittleFingerAnimation.Play();
        }

        if(rightIndexFinger != rightIndexFingerState)
        {
            rightIndexFingerState = rightIndexFinger;
            foreach (AnimationState state in rightIndexFingerAnimation)
            {
                state.speed = rightIndexFinger ? animationSpeed : -animationSpeed;
                state.time = rightIndexFinger ? 0 : state.length;
            }
            rightIndexFingerAnimation.Play();
        }
        if(rightMiddleFinger != rightMiddleFingerState)
        {
            rightMiddleFingerState = rightMiddleFinger;
            foreach (AnimationState state in rightMiddleFingerAnimation)
            {
                state.speed = rightMiddleFinger ? animationSpeed : -animationSpeed;
                state.time = rightMiddleFinger ? 0 : state.length;
            }
            rightMiddleFingerAnimation.Play();
        }
        if(rightRingFinger != rightRingFingerState)
        {
            rightRingFingerState = rightRingFinger;
            foreach (AnimationState state in rightRingFingerAnimation)
            {
                state.speed = rightRingFinger ? animationSpeed : -animationSpeed;
                state.time = rightRingFinger ? 0 : state.length;
            }
            rightRingFingerAnimation.Play();
        }
        if(rightLittleFinger != rightLittleFingerState)
        {
            rightLittleFingerState = rightLittleFinger;
            foreach (AnimationState state in rightLittleFingerAnimation)
            {
                state.speed = rightLittleFinger ? animationSpeed : -animationSpeed;
                state.time = rightLittleFinger ? 0 : state.length;
            }
            rightLittleFingerAnimation.Play();
        }

        Quaternion tempLeftQuat = new Quaternion();
        tempLeftQuat.w = -leftKaiQuat.w;
        tempLeftQuat.x = leftKaiQuat.y;
        tempLeftQuat.y = leftKaiQuat.z;
        tempLeftQuat.z = -leftKaiQuat.x;
        leftOrientationObject.localRotation = tempLeftQuat;

        Quaternion tempRightQuat = new Quaternion();
        tempRightQuat.w = -rightKaiQuat.w;
        tempRightQuat.x = rightKaiQuat.y;
        tempRightQuat.y = rightKaiQuat.z;
        tempRightQuat.z = -rightKaiQuat.x;
        rightOrientationObject.localRotation = tempRightQuat;

        if(!leftIndexFinger && leftMiddleFinger && leftRingFinger && leftLittleFinger)
        {
            currentTimeout -= Time.deltaTime;
            if(currentTimeout <= 0f)
            {
                // Shoot
                currentTimeout = shootTimeout;
                GameObject newBullet = GameObject.Instantiate(
                    bullet,
                    leftOrientationObject.position + (leftOrientationObject.forward * -12) + (leftOrientationObject.right * -2.2f),
                    leftOrientationObject.localRotation
                );
                newBullet.GetComponent<Rigidbody>().velocity = leftOrientationObject.forward * bulletSpeed * -1;
            }
        }
        if(!rightIndexFinger && rightMiddleFinger && rightRingFinger && rightLittleFinger)
        {
            currentTimeout -= Time.deltaTime;
            if(currentTimeout <= 0f)
            {
                // Shoot
                currentTimeout = shootTimeout;
                GameObject newBullet = GameObject.Instantiate(
                    bullet,
                    rightOrientationObject.position + (rightOrientationObject.forward * -12) + (rightOrientationObject.right * -2.2f),
                    rightOrientationObject.localRotation
                );
                newBullet.GetComponent<Rigidbody>().velocity = rightOrientationObject.forward * bulletSpeed * -1;
            }
        }
    }
    
    void OnLeftFingerShortcutData(object sender, FingerShortcutEventArgs args)
    {
        leftIndexFinger = args.IndexFinger;
        leftMiddleFinger = args.MiddleFinger;
        leftRingFinger = args.RingFinger;
        leftLittleFinger = args.LittleFinger;
    }

    void OnLeftQuaternionData(object sender, QuaternionEventArgs args)
    {
        leftKaiQuat.w = args.Quaternion.w;
        leftKaiQuat.x = args.Quaternion.x;
        leftKaiQuat.y = args.Quaternion.y;
        leftKaiQuat.z = args.Quaternion.z;
    }
    
    void OnRightFingerShortcutData(object sender, FingerShortcutEventArgs args)
    {
        rightIndexFinger = args.IndexFinger;
        rightMiddleFinger = args.MiddleFinger;
        rightRingFinger = args.RingFinger;
        rightLittleFinger = args.LittleFinger;
    }

    void OnRightQuaternionData(object sender, QuaternionEventArgs args)
    {
        rightKaiQuat.w = args.Quaternion.w;
        rightKaiQuat.x = args.Quaternion.x;
        rightKaiQuat.y = args.Quaternion.y;
        rightKaiQuat.z = args.Quaternion.z;
    }
}
