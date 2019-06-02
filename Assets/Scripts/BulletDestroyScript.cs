using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletDestroyScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DeleteObject());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator DeleteObject()
    {
        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }
}
