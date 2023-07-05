using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fists : Weapon {
    // Unarmed Fists, the default weapon

    protected override void AfterStart() {
        lightAttack = new Attack {
            comboDelay = 0.5f,
            swings = new List<Swing> {
                new Swing {
                    startPos = limb.ChinPos(),
                }, new Swing {
                    startPos = limb.ChinPos() + limb.transform.up * -.1f,
                }, new Swing {
                    startPos = limb.RaisedPos(),
                }, new Swing {
                    startPos = limb.ChinPos(),
                },
            }
        };

        heavyAttack = new Attack {
            comboDelay = 1f,
            swings = new List<Swing> {
                new Swing {
                    startPos = limb.RaisedPos(),
                },
                new Swing {
                    startPos = limb.RaisedPos(),
                },
            }
        };

        specialAttack = new Attack {
            comboDelay = 1f,
            swings = new List<Swing> {
                new Swing {
                    startPos = limb.ChinPos(),
                    speed = 3f,
                    swingDelay = 1f
                }
            }
        };
    }
}
