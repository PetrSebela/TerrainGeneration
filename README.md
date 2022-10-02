# Terrain generation


# Chunk generation system layout
```mermaid
graph TD;

    A1((Method call))
    B1(Creating height map)
    C1(Contrtucting mesh)
    D1(Adding detail)
    
    A1 --> B1
    B1 --> C1
    C1 --> D1

```