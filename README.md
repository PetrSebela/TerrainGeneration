# Terrain generation
This project will serve as my high school final thesis and is focused on problematics of procedural generation.
I would like to adress performance from beginning and build everything with it in mind. 

This project is currently in sub beta state, so lot of features are missing.

---
## Target for beta release
- basic terrain generation
    - terrain will be limited to just island generation 
- dynamic LOD system
    - LOD for terrain (currently in development | mostly done)
    - LOD for foliege
- exposed setting for terrain generation

## Generation pipeline
- GPU compatible algorithm

## Proc-Gen algorithm 
1. sample heightmap (currently working on)
2. apply falloff map
3. generate infrastructure
    - roads, powerlines, houses

    - powerlines
        generate points across the map that follow certain rules
        connect these points together based on how far are they apart

4. generate foliege

## How it works (order of execution)
( ChunkManager )
    ( start ) -> ( Generate World )
    - variables setup
    - calls ( Generation Manager ) coturine
        ( Generation Manager )
            - runs through every possible chunk
                - generates heightmap and enviromental details

            - starts ( Mesh Construction Manager ) in separate thread
                - runs forever

            ( Mesh Constructor Manager )
                - check if update requestQueue has any requests
                    - if yes, it calculates needed data and calls ( GesMeshData )
                    ( Get Mesh Data )
                        - internaly calls ( MeshConstructor.ConstructTerrain ) and bundles some data with it
                        ( MeshConstructor.ConstructTerrain )
                            - Constructs mesh
                    - after that it enques ChunkUpdate in [ MeshQueue ]

            - puts some data into requestQueue and waits till ( Mesh Constructor Manager ) finised procesing all data
            - instantiates all chunks and puts them into [ ChunkObjectDictionary ]
            - spawns water chunks
    ( update )
    - start only ( Generation Manager ) finishes 

## TASKS
- [ ] Implement quadtree algorythm for chunk grouping in order to save batch calls viz subdivision.png

## ProcGen algorithm design task
- [ ] Mesh instanced trees
- [ ] Island outline
- [ ] Paths, houses, power lines, infrastructure


# IDEAS
## Reducing GPU call count
- scrap quad tree division, keep generation as it is but group existing meshes into one based on LOD index and neighbours. 
- Sort of like static batching but custom and working (Unity's is broken).