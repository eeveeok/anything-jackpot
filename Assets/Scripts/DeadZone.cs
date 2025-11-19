using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadZone : MonoBehaviour
{

    void Dead<T>(T hitInfo)
    {
        //TODO 사망처리
        //무적 시간, 스폰포인트로 positon 옮기기, 무적 시간 동안 alpha 변경
    }

    private void OnCollisionEnter(Collision collision)
    {
        Dead(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        Dead(other);
    }
}
