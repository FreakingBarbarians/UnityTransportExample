using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : NetworkedTransform {
    private void Update()
    {
        if (!LocalAuthority) {
            return;
        }

        Vector3 moveVec = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) {
            moveVec.y++;
        }
        if (Input.GetKey(KeyCode.S)) {
            moveVec.y--;
        }
        if (Input.GetKey(KeyCode.A)) {
            moveVec.x--;
        }
        if (Input.GetKey(KeyCode.D)) {
            moveVec.x++;
        }
        transform.position += moveVec * Time.fixedDeltaTime;
    }

}
