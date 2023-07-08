using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fists : Weapon {
    // Unarmed Fists, the default weapon

    protected override void AfterStart() {
        // lightAttack = new Attack(this) {
        //     comboDelay = 0.5f,
        //     strikes = new List<Strike> {
        //         new Strike {
        //             startPos = limb.landmarks.ChinPos(),
        //         }, new Strike {
        //             startPos = limb.landmarks.ChinPos() + limb.transform.up * -.1f,
        //         }, new Strike {
        //             startPos = limb.landmarks.RaisedPos(),
        //         }, new Strike {
        //             startPos = limb.landmarks.ChinPos(),
        //         },
        //     }
        // };

        // heavyAttack = new Attack {
        //     comboDelay = 1f,
        //     strikes = new List<Strike> {
        //         new Strike {
        //             startPos = limb.landmarks.RaisedPos(),
        //         },
        //         new Strike {
        //             startPos = limb.landmarks.RaisedPos(),
        //         },
        //     }
        // };

        // specialAttack = new Attack {
        //     comboDelay = 1f,
        //     strikes = new List<Strike> {
        //         new Strike {
        //             startPos = limb.landmarks.ChinPos(),
        //             speed = 3f,
        //             strikeDelay = 1f
        //         }
        //     }
        // };
    }
}
