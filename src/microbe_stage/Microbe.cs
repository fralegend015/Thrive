using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Main script on each cell in the game
/// </summary>
public class Microbe : RigidBody, ISpawned, IProcessable, IMicrobeAI
{
    /// <summary>
    ///   The stored compounds in this microbe
    /// </summary>
    public readonly CompoundBag Compounds = new CompoundBag(0.0f);

    /// <summary>
    ///   The point towards which the microbe will move to point to
    /// </summary>
    public Vector3 LookAtPoint = new Vector3(0, 0, -1);

    /// <summary>
    ///   The direction the microbe wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection = new Vector3(0, 0, 0);

    private CompoundCloudSystem cloudSystem;
    private Membrane membrane;

    /// <summary>
    ///   The species of this microbe
    /// </summary>
    public MicrobeSpecies Species { get; private set; }

    public int HexCount
    {
        get
        {
            // TODO: add computation and caching for this
            return 1;
        }
    }

    public int DespawnRadiusSqr { get; set; }

    public Node SpawnedNode
    {
        get
        {
            return this;
        }
    }

    // TODO: implement process list
    public List<TweakedProcess> ActiveProcesses { get; private set; } =
        new List<TweakedProcess>();

    public CompoundBag ProcessCompoundStorage
    {
        get { return Compounds; }
    }

    public float TimeUntilNextAIUpdate { get; set; } = 0;

    /// <summary>
    ///   For use by the AI to do run and tumble to find compounds
    /// </summary>
    public Dictionary<string, float> TotalAbsorbedCompounds { get; set; } =
        new Dictionary<string, float>();

    /// <summary>
    ///   Must be called when spawned to provide access to the needed systems
    /// </summary>
    public void Init(CompoundCloudSystem cloudSystem)
    {
        this.cloudSystem = cloudSystem;
    }

    public override void _Ready()
    {
        if (cloudSystem == null)
        {
            throw new Exception("Microbe not initialized");
        }

        membrane = GetNode<Membrane>("Membrane");

        // TODO: reimplement capacity calculation
        Compounds.Capacity = 50.0f;
    }

    /// <summary>
    ///   Applies the species for this cell. Called when spawned
    /// </summary>
    public void ApplySpecies(Species species)
    {
        Species = (MicrobeSpecies)species;
        ResetOrganelleLayout();
        SetInitialCompounds();
    }

    public void ResetOrganelleLayout()
    {
        // TODO: send hexes to membrane
        // membrane
    }

    /// <summary>
    ///   Resets the compounds to be the ones this species spawns with
    /// </summary>
    public void SetInitialCompounds()
    {
        Compounds.ClearCompounds();

        foreach (var entry in Species.InitialCompounds)
        {
            Compounds.AddCompound(entry.Key, entry.Value);
        }
    }

    public override void _Process(float delta)
    {
        if (MovementDirection != new Vector3(0, 0, 0))
        {
            // Movement direction should not be normalized to allow different speeds
            Vector3 totalMovement = new Vector3(0, 0, 0);

            totalMovement += DoBaseMovementForce(delta);

            ApplyMovementImpulse(totalMovement, delta);
        }

        // ApplyRotation();

        HandleCompoundAbsorbing();

        HandleCompoundVenting();
    }

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        // TODO: should movement also be applied here?

        state.Transform = GetNewPhysicsRotation(state.Transform);
    }

    private void HandleCompoundAbsorbing()
    {
        float scale = 1.0f;

        if (Species.IsBacteria)
            scale = 0.5f;

        // This grab radius version is used for world coordinate calculations
        // TODO: switch back to using the radius from membrane
        float grabRadius = 3.0f;

        // // max here buffs compound absorbing for the smallest cells
        // const auto grabRadius =
        //     std::max(membrane.calculateEncompassingCircleRadius(), 3.0f);

        cloudSystem.AbsorbCompounds(Translation, grabRadius * scale, Compounds,
            TotalAbsorbedCompounds);
    }

    /// <summary>
    ///   Vents (throws out) non-useful compounds from this cell
    /// </summary>
    private void HandleCompoundVenting()
    {
        // Skip if process system has not run yet
        if (!Compounds.HasAnyBeenSetUseful())
            return;
    }

    private Vector3 DoBaseMovementForce(float delta)
    {
        var cost = (Constants.BASE_MOVEMENT_ATP_COST * HexCount) * delta;

        var got = Compounds.TakeCompound("atp", cost);

        float force = Constants.CELL_BASE_THRUST;

        // Halve speed if out of ATP
        if (got < cost)
        {
            // Not enough ATP to move at full speed
            force *= 0.5f;
        }

        return Transform.basis.Xform(MovementDirection * force);

        // * microbeComponent.movementFactor *
        // (SimulationParameters::membraneRegistry().getTypeData(
        // microbeComponent.species.membraneType).movementFactor -
        //     microbeComponent.species.membraneRigidity *
        // MEMBRANE_RIGIDITY_MOBILITY_MODIFIER));
    }

    private void ApplyMovementImpulse(Vector3 movement, float delta)
    {
        if (movement.x == 0.0f && movement.z == 0.0f)
            return;

        ApplyCentralImpulse(movement * delta);
    }

    /// <summary>
    ///   Just slerps towards a fixed amount the target point
    /// </summary
    private Transform GetNewPhysicsRotation(Transform transform)
    {
        var target = Transform.LookingAt(LookAtPoint, new Vector3(0, 1, 0));

        return new Transform(Transform.basis.Slerp(target.basis, 0.2f), Transform.origin);
    }
}
