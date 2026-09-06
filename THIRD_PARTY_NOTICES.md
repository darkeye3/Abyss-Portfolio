# Original game, algorithm and asset references

## Darkest Dungeon

The original game is **Darkest Dungeon**, developed by **Red Hook Studios**.
[Official game page](https://www.darkestdungeon.com/darkest-dungeon/).

Abyss is a Unity/C# reimplementation project for learning and a game programming portfolio.
Its combat, exploration and estate-management rules and content structure reference Darkest Dungeon.
The rules were organized using reverse-engineering notes from an existing Unity reimplementation.
The portfolio presents the implementation work on Abyss, including its C# rules architecture,
save/restore behavior, Unity integration and tests.

In the technical documents, "original source" refers to the full Abyss project from which
the public code samples and excerpts were selected.

## PCG

`Pcg32RandomSource` implements a PCG32-style generator. The PCG family of algorithms was
designed by Melissa E. O'Neill. Algorithm description and reference implementations:

- [PCG Random](https://www.pcg-random.org/)
- [PCG paper](https://www.pcg-random.org/paper.html)

PCG32 is an established algorithm; this portfolio does not claim to have invented it.
The engineering example here is the C# interface, named-stream integration, snapshot
restoration, and their use in reproducible gameplay.

## Screenshots

The PNG files in `images/` are UI render captures of Abyss. Project visual assets include
images produced with generative image tools and subsequent preparation for Unity.
They are included as presentation material rather than an asset package.

## Dependencies

The runnable C# sample uses the .NET standard libraries and has no external NuGet packages.
