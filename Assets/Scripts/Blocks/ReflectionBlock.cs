﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReflectionBlock : Block {

    public override void LaserDamage(Collision2D block, GameObject projectile){
        base.LaserDamage (block, projectile);
        ReflectLaser (block, projectile);
    }

    public override void ReflectLaser (Collision2D block, GameObject projectile) {
        // Normal of collider surface
        Vector3 normal = block.contacts[0].normal;
        // Reflect laser
        projectile.GetComponent<Rigidbody2D>().velocity = Vector3.Reflect(rigid.velocity, normal).normalized * projectile.GetComponent<Projectile>().bullet_speed;
        // Change layer to other team
        projectile.layer = (projectile.layer == LayerMask.NameToLayer("Team1Projectiles")) 
            ? LayerMask.NameToLayer("Team2Projectiles") 
            : LayerMask.NameToLayer("Team1Projectiles");
    }
}