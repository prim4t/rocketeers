﻿    /*  Author: SK && CG
 *
 *  Update (3/5/17): Make block stick to any neighbors it has on creation
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Direction {
    NORTH,
    SOUTH,
    EAST,
    WEST
}

public enum BlockStates {
    FALLING,
    STILL,
    UNHINGED,
}

// This is to encapsulate the FixedJoint2D of a 'hinge' and the block that it is connected to.
// This is the value of the connected_neighbors map used for connecting and disconnecting from
// blocks
public struct FixedJointContainer {

    public FixedJoint2D fixed_joint;
    public Block block;

    public FixedJointContainer(FixedJoint2D in_fixed_joint, Block in_block){
        this.fixed_joint = in_fixed_joint;
        this.block = in_block;
    }
}

public class Block : MonoBehaviour {
    // Inspector manipulated attributes
    // JF: Contains layers of blocks from all teams for attachment purposes
    public LayerMask                            allBlocksMask;
    public float                                snap_radius = 0.75f;
    public GameObject                           highlight_sprend;
    public LayerMask                            placementMask;
    public GameObject                           explosion;
    public bool                                 ______________________;

    // JF: Team this block is assigned to. Inherit from blocks it first connects to
    public int                                  teamNum = 0;

    // Encapsulated attributes
    public bool                                 being_manipulated;
    public bool                                 in_placeable_spot = false;
    public bool                                 block_fell = false;
    protected Health                            health;

    // GameObject components & child objects
    protected Rigidbody2D                       rigid;

    // JF: Image object to display sprites on the block, such as a tooltip
    public Image                               image;


    // Neighbor joints
    public Dictionary<Direction, FixedJointContainer>         connected_neighbors = new Dictionary<Direction, FixedJointContainer>();
    public Dictionary<Direction, GameObject>    highlight_map = new Dictionary<Direction, GameObject>();


    // Block states
    public Dictionary<BlockStates, Action>      states = new Dictionary<BlockStates, Action>();
    private BlockStates                         state = BlockStates.FALLING;

    private float                               SLEEPING_THRESHOLD = 0.1f;
    private float                               MINIMUM_RAMMING_VELOCITY = 3.9f;



    void Awake(){

        rigid = GetComponent<Rigidbody2D> ();
        health = GetComponent<Health> ();
        health.SetParent (this);

        states.Add (BlockStates.FALLING, Falling);
        states.Add (BlockStates.STILL, Still);
        states.Add (BlockStates.UNHINGED, Unhinged);

    }

    // Use this for initialization
    void Start () {

        // JF: Get image object if core
        if (tag == "Core") {
            image = transform.Find("Canvas").Find("Image").GetComponent<Image> ();
        }
        ShowAvailablePlaces ();
        // SK: Don't want rockets and core to add joints
        if (tag == "Rockets" || tag == "Core") {
            state = BlockStates.STILL;
            return;
        }
        CheckForAnyNeighbors ();
    }

    protected virtual void Update(){
        // run the correct state function each update
        states [state] ();
    }

    /******************** State Modifiers & Behaviors ********************/

    // Block is in this state if it is falling
    // Entered from: Block creation or surrounding block destruction
    // Exits to: Still()
    void Falling(){

        // check if the block has come to a rest
        if (rigid.velocity.magnitude <= SLEEPING_THRESHOLD) {
            state = BlockStates.STILL;
            return;
        }

        // block is still falling, any logic that needs to be performed can be put here.
        // At the moment, I can't think of anything the block needs to do when it is falling though
    }

    // When a block is not moving, or not just previously moving
    // Entered from: Falling()
    // Exits to: Falling(), OnDestroy() (indirectly)
    void Still(){
        // the block is moving again, so transition to the falling state
        if (rigid.velocity.magnitude > SLEEPING_THRESHOLD) {
            if (tag != "Rockets" || tag == "Core") {
                state = BlockStates.FALLING;
            }
            return;
        }
    }

    // unhinges the block from neighbors
    // Called by: Health.CheckToDestroy()
    // Entered from: Falling(), Still()
    // Exits to: Unhinged()
    public virtual void UnhingeAndFall(){
        Unhinge ();
        Fall ();
        state = BlockStates.UNHINGED;
    }

    // this is run once a block has no health and falls down
    // Entered from: UnhingeAndFall()
    // Exits to: nothing
    void Unhinged(){
        // check and destroy us if we're offscreen
        DestroyIfBelowScreen ();
    }

    // Calling condition: when a projectile collides with a block
    // Called by: Projectile.OnTriggerEnter2D()
    public virtual void LaserDamage(Collision2D block, GameObject projectile){
        health.Damage(health.DAMAGE_FROM_LASER);
        SFXManager.GetSFXManager ().PlaySFX (SFX.BlockHit);
        DestroyProjectile (projectile);
    }

    protected virtual void DestroyProjectile(GameObject projectile){
        Destroy (projectile);
    }

    public virtual void ExplosionDamage () {
        if (health)
            health.ExplosiveDamage ();
    }

    public virtual void RammingDamage(float bonus){
        SFXManager.GetSFXManager ().PlaySFX (SFX.ShipImpact);
        health.RammingDamage (bonus);
    }

    // called when this block has <= 0 health
    public virtual void Kill(){
        UnhingeAndFall ();
    }

    protected virtual void OnCollisionEnter2D(Collision2D other){
        if (rigid.velocity.magnitude < MINIMUM_RAMMING_VELOCITY){
            return;
        }

        Block otherBlock = other.gameObject.GetComponent<Block> ();

        if (otherBlock) {
            otherBlock.RammingDamage (rigid.velocity.magnitude);
            // do the base amount of ramming damage to ourselves, no multipliers applied
            RammingDamage (0f);
        }
    }


    /******************** Utility ********************/

    void ShowAvailablePlaces(){

        HashSet<Direction> all_dirs = Utils.GetAllDirections ();
        foreach (Direction dir in all_dirs) {
            Collider2D blocker = Physics2D.OverlapCircle (transform.position + Utils.DirToVec(dir),
                                                          0.3f,
                                                          placementMask);
            Vector3 to_set_pos = transform.position + Utils.DirToVec (dir);
            // no neighbor here so display the highlight
            if (blocker == null && Utils.ValidBlockLocation(to_set_pos)) {
                GameObject go = Instantiate<GameObject> (highlight_sprend,
                                                         to_set_pos,
                                                         Quaternion.identity);
				go.transform.parent = transform;
                highlight_map.Add (dir, go);
            }
        }
    }

    // Calling condition: check and destroy this block if it's offscreen
    // Called by: this.Update()
    void DestroyIfBelowScreen(){
        if (MainCamera.S.IsBelowScreen (transform.position)) {
            Destroy (gameObject);
        }
    }

    // Calling condition: Checking for any block in all four directions to connect to
    // Called by: this.Start()
    public void CheckForAnyNeighbors(){
        HashSet<Direction> all_dirs = Utils.GetAllDirections ();
        foreach (Direction dir in all_dirs) {
            CheckAndConnectToNeighbor (dir);
        }
    }


    // Calling Condition: Check for a surrounding neighbor and connect to it
    // Called by: this.Start()
    void CheckAndConnectToNeighbor(Direction dir){
        Block neighbor = null;
        int neighborTeamNum = CheckForNeighbor (dir, out neighbor);
        if (neighborTeamNum > 0) {
            if (!connected_neighbors.ContainsKey (dir)) {
                // we have a neighbor, so connect to it
                ConnectToNeighbor (dir, neighbor);
                // connect the neighbor in the opposite direction, since that's the side
                // this block is on
                neighbor.ConnectToNeighbor (Utils.GetOppositeDirection (dir), this);

                // JF: Assign teamNum to this block according to neighborTeamNum
                AssignTeamToBlock (this, neighborTeamNum);
            }
        }
    }

    // Calling condition: A neighboring block dies
    // Called by: neighboring block - not invoked by this on itself.
    public void DeleteNeighboringConnection(Direction dir){
        // CG: I've determined that it's plausible this function could be called on two neighboring blocks
        // in the same frame and thus one will run fine, but the other will throw what used to be a Debug.Assert
        // because the other neighbor will have already been destroyed. Instead, just return and assume the
        // fixed joint has been taken care of
        if (!connected_neighbors.ContainsKey (dir)) {
            return;
        }
        // need to remove the fixedjoint in this direction, not just the direction from the map
        if (connected_neighbors.ContainsKey(dir)) {
            Destroy(connected_neighbors[dir].fixed_joint);
            connected_neighbors.Remove (dir);
        }
    }

    // removes all connections from this block and the connections to other neighbors around us
    public void DeleteAllNeighboringConnections(){
        // for each neighbor around us
        foreach (KeyValuePair<Direction, FixedJointContainer> dir in connected_neighbors) {

            // when the ground is removed, the 'block' value is null
            if (dir.Value.block != null) {
                // since we're removing ourself from our neighbors, the directions
                // are reversed
                dir.Value.block.DeleteNeighboringConnection (Utils.GetOppositeDirection (dir.Key));
            }
            // Destroy our own FixedJoint2D
            Destroy (dir.Value.fixed_joint);
        }
    }

    // Calling condition: When a block falls. The direction passed in is not
    //                    already present in the direction map
    // Called by: FallingToStill()
    // [JF] Returns: Team the neighbor block is assigned to
    public int CheckForNeighbor(Direction dir, out Block neighbor_block) {
        int teamNum = 0;
        Collider2D neighbor = Utils.CheckForObj(transform.position + Utils.DirToVec(dir),
                                                allBlocksMask);
        if (neighbor != null) {
            neighbor_block = neighbor.gameObject.GetComponent<Block>();
            teamNum = neighbor_block.teamNum;
        }
        else {
            neighbor_block = null;
        }
        return teamNum;
    }

    // Calling condition: When either another block or this block has fallen and
    //                    finds an existing neighbor to connect to.
    // Called by: this and neighboring block simultaneously
    public void ConnectToNeighbor(Direction dir, Block other){
        // add the fixedjoints and update the direction map
        FixedJoint2D fj = AddFixedJoint (other.gameObject);
        connected_neighbors.Add(dir, new FixedJointContainer(fj, other));
        // destroy the highlight map
        // the newest block being placed will not have the highlight direction in it
        if (highlight_map.ContainsKey (dir)) {
            Destroy (highlight_map [dir]);
            highlight_map.Remove (dir);
        }
    }

    // JF: Swaps this block with another 
    public void SwapWithBlock(Block otherBlock) {
        // Detach both from their neighbors
        DeleteAllNeighboringConnections ();
        otherBlock.DeleteAllNeighboringConnections ();

        // Swap their positions
        Vector3 loc = otherBlock.transform.position;
        otherBlock.transform.position = transform.position;
        transform.position = loc;

        // Check for new neighbors
        CheckForAnyNeighbors ();
        otherBlock.CheckForAnyNeighbors ();
    }

    // JF: Assigns block to a team and modifies its layers to match
    public void AssignTeamToBlock(Block block, int teamNum) {
        block.teamNum = teamNum;
        block.gameObject.layer = LayerMask.NameToLayer ("Team" + teamNum + "Block");

        block.GetComponent<BoxCollider2D> ().enabled = false;
        block.GetComponent<BoxCollider2D> ().enabled = true;

        // Assign platform layers
        foreach (Transform t in block.transform) {
            t.gameObject.layer = LayerMask.NameToLayer ("Team" + teamNum + "Platform");
        }
        // CG: Make blocks impenetrable to the other team
        int opposite_team_num = Utils.GetOppositeTeamNum (teamNum);
        block.transform.Find ("BlockerCollider").gameObject.layer = LayerMask.NameToLayer ("ImpenetrableToTeam" + opposite_team_num);
    }

    void ConnectToGround(Direction dir, GameObject ground){
        // add the fixedjoints and update the direction map
        FixedJoint2D fj = AddFixedJoint (ground);
        connected_neighbors.Add(dir, new FixedJointContainer(fj, null));
    }

    // Calling condition: when needing to add a fixed joint from this gameObject to another gameObject
    // Called by: ConnectToNeighbor()
    FixedJoint2D AddFixedJoint(GameObject other_go){
        FixedJoint2D fj = gameObject.AddComponent<FixedJoint2D> ();
        fj.connectedBody = other_go.GetComponent<Rigidbody2D> ();
        return fj;
    }

    // removes all FixedJoints
    void Unhinge(){

        // JF: EXPLOSIONS!
        if (explosion != null) {
            GameObject boom0 = Instantiate(explosion, transform.position, Quaternion.identity);
            boom0.transform.localScale = Vector3.one / 2f;
            boom0.GetComponent<LoopingAnimation>().StartAnimation();
        }

        SFXManager.GetSFXManager ().PlaySFX (SFX.SmallExplosion);

        DeleteAllNeighboringConnections ();

    }

    // causes the block to lose it's constraints and fall through other layers
    void Fall(){
        gameObject.layer = LayerMask.NameToLayer ("TransparentFX");
        GetComponent<BoxCollider2D> ().enabled = false;
        // remove our children so they don't interfere with collisions
        RemoveChildren ();
        RemoveConstraints ();
    }

    // removes our rotation and position constraints
    void RemoveConstraints(){
        // allow some rotation to make it more juicy
        rigid.constraints = RigidbodyConstraints2D.None;
        rigid.angularVelocity = UnityEngine.Random.Range(-50f, 50f);
    }

    // destroys all the blocks children
    void RemoveChildren(){
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));
    }

    // removes all the highlights from the block
    public void RemoveHighlights(){
        foreach(Direction dir in highlight_map.Keys){
            Destroy (highlight_map [dir]);
        }
        highlight_map.Clear ();
    }

    void OnDestroy(){
        RemoveHighlights ();
    }

    public void SetJellyMode(){
        rigid.constraints = RigidbodyConstraints2D.None;
    }
}
