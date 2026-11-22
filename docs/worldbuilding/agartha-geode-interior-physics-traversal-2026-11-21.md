# Dungeons & Crows: Agartha Geode Interior
## Design memo AGARTHA-003 | Continuous traversal and physics contract

**Date:** 2026-11-21 (America/Chicago)  
**Project:** Dungeons & Crows (same game, not a spin-off)  
**Status:** USER-DIRECTED FICTIONAL WORLDBUILDING / IMPLEMENTATION SPECIFICATION, NOT BUILT OR PHYSICALLY VALIDATED  
**Supersedes:** none. Expands the existing Voice-to-Vision and inverted-world design while preserving previously approved combat and character canon.

### 1. Canonical visual premise
A world whose interior resembles a planetary-scale **geode**: crystalline spires, ridges, fjords, oceans, mountain walls, and fractured ravines occupy the inward-facing shell. At an immense distance, land beyond the horizon appears to curve *upward* across the interior. It must ultimately be reachable in the same world, using real traversable distances and continuous coordinates rather than fake skybox destinations.

At its center is a **small, luminous, rotating ball of gas/plasma**. Its *position is stationary relative to the interior-world reference frame*, but it rotates about its own axis. Surface activity such as prominences and flares is part of the visible fiction and may influence the simulated radiation/electromagnetic environment where the model supports it. The sun does **not** orbit around the player or around the shell. A proposed **persistent-eclipse** appearance is a separate geometry/occlusion question, not an assumed physical consequence of a stationary sun.

Mountains and crystal spires point inward toward the central light. The high exposed peaks near the light are imagined as **hot, bright, arid high deserts**. The lower, shadowed, protected or thermally isolated chasms hold **glaciers, frozen ravines, ice caverns, and cold rivers**. Fjords cut between immense walls, joining freshwater and saline basins, caves and vast interior oceans. All zones should be distinguishable through illumination, weathering, rock and crystal forms, vegetation and materials, not a single repeated architectural style.

### 2. Spatial continuity and traversal
- One coherent 3D planetary coordinate system contains outer-world approach routes, hypothetical apertures, interior landscapes, and deep caverns.
- The player can hike, scramble, climb, ride, glide or sail **along the physical geometry** of mountains, fractures, bridged precipices and shoreline passages.
- Route topology: frozen fjord floor -> crystalline lower terraces -> ascending sunward spine -> exposed hot spire desert -> cracked saddle -> narrow ravine -> adjacent mountain -> downstream cave/lake network.
- The route can double back, fork, or reconnect after long distances; no silent teleport disguised as movement. Fast travel may exist separately if unlocked and disclosed.
- Distant areas are genuine spatial destinations with streamed meshes, collision and navigation once approached. Use hierarchical LOD/impostors only for appearance at long range.
- “Fall up a mountain” is permitted as an **emergent motion** only if local calculated acceleration, trajectory and contact geometry actually cause it; never arbitrarily change the gravity vector to force a gag.

### 3. Physics integrity gate
The user requests *real physics applied to a speculative topology*, rather than unaccountable magic. Keep these separate:
- **Established equations:** Newtonian gravity, inertial/rotating reference frames, fluid flow, radiative flux, heat balance, electromagnetism.
- **Speculative initial conditions:** Earth-sized hollow cavity, a small central radiating plasma body, polar apertures, extremely tall inward mountains, and any perpetual eclipse arrangement.
- **Computed outcomes:** trajectories, traction, free fall, fluid equilibrium, magnetic effects, and thermal distributions from explicitly selected parameters. If a premise cannot coexist with the equations, record the contradiction; do not rename it “real physics.”

Crucial constraint: an ideal spherically symmetric hollow shell has **zero gravitational field inside the cavity** (shell theorem). It does *not* naturally hold people to the inner ground. A rotating shell produces outward centrifugal acceleration relative to its spin axis, but that is latitude-dependent, vanishes at the axis, and is not spherical-surface gravity. A central massive sun would attract objects **toward the center**, opposite inner-surface support. Therefore a physically honest prototype must calculate net forces from the chosen mass geometry, rotation, celestial masses, and local structures, and test where walkable ground is actually possible. It must **not** advertise a geode Earth as an established geological model.

Suggested simulation: specify reference frame and rotation axis; evaluate
`a = a_grav_shell + a_grav_center + a_grav_local + a_rotating_frame + a_external`.
Use continuous vector fields; contact and material forces emerge from collisions and ground normals; trajectories integrate the local acceleration at each physics step. For UI readability, distinguish **world radial-inward**, **player's perceived up**, **surface normal**, and **acceleration/down**. These may disagree.

For thermal regions, a closer exposed spire can receive stronger irradiance (`flux ~ luminosity/(4*pi*r^2)`) but temperature also depends on occlusion, atmosphere, conduction, convection and water/ice balance. Heat-at-tip / ice-at-base is a **target design arrangement to evaluate**, not a universal prediction.

Magnetic lines are closed/conserved-field geometries, not rays that terminate at peaks. Minerally rich spires may create local magnetic anomalies if their material, magnetization and conductivity support them. Strong magnetic effects on ordinary wooden vessels or aircraft are not assumed. Electrical conductivity, actual forces, and navigation sensor response must be computed.

An eclipse is about *occlusion geometry*. For a stationary central sun, a fixed ring, moon, arch, mountain silhouette or engineered structure could produce persistent shadow or corona for **some** viewpoints, but a permanent identical eclipse for **every** viewpoint is not silently assumed. Model the shadow volume, move the observer, and test visibility.

### 4. Environmental palette and phenomena
**Crownspires (sunward):** hot mineral deserts, bright quartz walls, crystallized salt terraces, dust devils, wind-cut ledges, spectral mirages with optical explanations when possible.

**Frostwounds (basal):** ice caves, glacial crevasses, hanging frozen lakes, snowfall collecting in shaded trenches, dripping caverns, moving glaciers.

**Fjord fractures:** water channels along giant clefts, waterfall cataracts, sheltered harbors, ice-bound ship routes, submerged caves; water flows wherever effective gravity and pressure allow, including unusual directions relative to a character standing on an adjacent surface.

**Skyward horizon:** distant inner continents, apparent overhead seas, ships or settlements seen across the cavity, enormous feature sizes with actual depth/parallax; do not confuse visible sky imagery with navigable terrain unless physically connected through world coordinates.

**Geode riblands:** giant crystalline clusters, mineral sheets and overhangs, caves carrying refracted light, acoustically enormous chambers.

**Solar-facing haze:** irradiated atmospheric layers, clouds, dust, coronal-looking light, and carefully bounded flare events. Solar activity can change visual atmosphere but should only alter gameplay physics where the simulated forcing is measurable.

### 5. Mission prototype: The Fjord-to-Spire Traverse
1. Player begins aboard a wooden exploratory vessel in a sheltered glacial fjord and selects a navigable landing.
2. Player can inspect current, drift and water depth; a waterfall may lead into a hypothesized polar aperture expedition in a future outer-world module. The ship is not treated as magnetically immune; the materials have specified properties.
3. Leave ship and traverse a long frozen ravine; light from the stationary sun filters between inward-pointing spires.
4. Climb an exposed hot crystal ridge. As the viewing angle shifts, an **overhead ocean** across the inner cavity resolves into a distant coast with an identifiable route.
5. At the ridge crest, a physics anomaly appears *only if the computed gravity/rotation field permits it*: a loose stone moves in a counterintuitive direction relative to terrain, inviting the player to test with thrown objects before risking a jump.
6. Descend into a fracture and take either an ice cave or a bridge to an adjoining mountain. Maintain a coherent actual position, elevation and route length.
7. Find a concealed treasure structure in a geode chamber; a Dungeon Master may change encounter scheduling or narrative context, but may not arbitrarily override the fixed integrator or erase earned loot.
8. A final overlook reveals the central rotating sun in a long-lasting partial corona, its apparent eclipse dependent on the occluder and viewpoint.

### 6. Engine contracts
- **World geometry:** interior spherical/irregular shell surface with near-field 3D chunk streaming, plus a calibrated distant curved inner-horizon renderer.
- **Coordinate math:** double precision planetary position, local floating-origin conversion for render physics; no discontinuities across chunks.
- **Gravity:** deterministic function `getAcceleration(position,time,worldParameters)` with explicit mass and rotation inputs, unit tests and visual vector-field debug overlay.
- **Movement:** controller locomotion relative to tangent plane and actual net acceleration; supports walking, climbing, sliding, slipping, falling, swimming, flying and mounted motion.
- **Water:** regional basins and currents from chosen potential/pressure field, not arbitrary water “climbing” a waterfall under normal Earth gravity.
- **Temperature:** surface irradiance + occlusion + atmosphere + heat transfer; freeze/melt thresholds consistent with simulation model.
- **Sun:** fixed world-space center; rotating surface shader/plasma visuals; distinct concepts of rotation, flare emission and translation.
- **Optical eclipse:** occluder volume and per-camera line-of-sight test, not a fixed black disk composited at all angles.
- **Dungeon Master:** seeded event choices with auditable bounds, reversible world-state transitions, no violations of physical conservation models without declaring a new fictional scenario assumption.
- **Mobile performance:** world scale simulated in streamed sectors; quality scalability; test actual device navigation, frame pacing, collisions and save/load.

### 7. First demonstrable acceptance tests (NOT YET RUN)
1. Inner shell far horizon visibly curves *upward*; moving camera changes apparent parallax and scale.
2. Center light remains fixed while visible plasma texture rotates. Test player position change and light direction.
3. Follow one continuous route from frozen fjord -> sun-exposed spire -> fracture -> neighboring landscape without forced teleport.
4. Record distances, positions, orientation and gravity vectors at 10 sample points, including the crest; test no unphysical vector jump.
5. Drop the same object from different mountain surfaces and compare observed trajectories to numerical predictions.
6. Confirm water basins and directional currents agree with chosen effective potential, or mark the desired waterfall as infeasible.
7. Demonstrate viewpoint-dependent eclipse/occlusion. Do not claim permanent global eclipse unless the geometry supports it.
8. Show a hidden loot room reached without a scene swap; save, exit and reload with gear and route position intact.
9. Run a deterministic automated test where the Dungeon Master changes an encounter but cannot change gravity rules or inventory ownership.
10. Report hardware FPS/performance and artifact evidence only after an actual build/run.

### 8. Design integrity
This is a **fictional-world design** constrained by real mathematical models, not evidence that Earth's interior is hollow or that polar flights/satellites are excluded by magnetic fields. Unresolved premises are explicitly left as questions to model/test. The primary aim remains a **playable, free-roaming Dungeons & Crows** action RPG, not a separate game, still featuring direct melee combat, faction conflicts, ghost-pause class changes, crow-head transmutation only during transformation, separate black-wing traversal, loot and voluntary Legacy Rebirth.

**Next engineering slice:** Implement a planetary-coordinate + vector-gravity visualization harness and one connected fjord-to-spire graybox. Verify actual walking trajectories before claiming realistic thermal/solar/magnetic behavior.
