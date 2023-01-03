using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class JumpScript : PoolItem
{
    

    public Vector2 jumpForceY = new Vector2(20,50);
    public Vector2 jumpForceX = new Vector2(1,5);
    public float gravityScale = 5;
    public float gravity = -9.81f;
    public float rotate = 3;
    public float minRotate = 3;
    public float velocity;
    float velocityX;
    public Transform rotateTransform;


    public float returnY = -50;

    public IndexBindingScript action;
    public void SetColorThenJump(int color,Vector3 position)
    {
        transform.position = position;
        velocity = Random.Range(jumpForceY.x, jumpForceY.y);
        velocityX = Random.Range(jumpForceX.x, jumpForceX.y);
        rotateTransform.rotation = Quaternion.identity;
        action.OnChanged(color);
        Observable.FromCoroutine(JumpCoroutine).Subscribe(data =>
        {
            //Return();
        });
    }
    
    
    public IEnumerator JumpCoroutine()
    {
        while (transform.position.y > returnY)
        {
            transform.Translate(new Vector3(velocityX, velocity, 0) * Time.deltaTime);
//            rotateTransform.rotation *= Quaternion.Euler(0, 0,new Vector3(velocityX, velocity, 0).magnitude * (velocityX >0 ? 1 : -1) * rotate);
//            rotateTransform.rotation *= Quaternion.Euler(0, 0,Mathf.Max(new Vector3(velocityX, velocity, 0).magnitude,minRotate) * (velocityX >0 ? 1 : -1) * rotate);
            rotateTransform.rotation *= Quaternion.Euler(0, 0,rotate);
            velocity += gravity * gravityScale * Time.deltaTime;
            yield return null;
        }
    }

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.Space))
//        {
//            gameObject.SetActive(true);
//            SetColorThenJump(0, Vector3.zero);
//        }
////        velocity += gravity * gravityScale * Time.deltaTime;
////        if (Input.GetKeyDown(KeyCode.Space))
////        {
////            transform.position = Vector3.zero;
////            velocity = Random.Range(jumpForceY.x, jumpForceY.y);
////            velocityX = Random.Range(jumpForceX.x, jumpForceX.y);
////        }
////        
////        if (Input.GetKeyDown(KeyCode.Escape))
////        {
////            transform.position = Vector3.zero;
////            velocity = 0;
////            velocityX = 0;
////            rotateTransform.rotation = Quaternion.identity;
////        }
//    }
}