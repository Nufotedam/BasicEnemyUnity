Basic Enemy UNITY 3D
----------------------------------
Tutorial project that shows the general configuration of the movement and control of a character and the basic configuration of an intelligence of an enemy, including patrolling, chasing and listening. All the intelligence of the enemy is programmed using C# and using the navigation agent of unity.
----------------------------------
The whole project has a single scene where there is a level with several obstacles. The main character will be able to perform the following movements:
- walk
- run
- jump
- falling down slopes

All character movements were made using the unity character controller and C# programming.

There is only one enemy in the scene that its default state is to patrol through certain points of the level. If the player is too close to the enemy and he has not seen him, the enemy will hear the player pass by and if the player is walking or running, the enemy will take one action or another. In the level also the player will find a safe zone where the enemy will not be able to capture the player if he is within that zone.

----------------------------------
You can find useful things in the code to learn how to perform the general movement of the main character and also the intelligence and movement of an enemy in a zone.
You can also access a FBX where you will find the model and the different animations of a Blender Mannequin.
----------------------------------
Software:
Unity version: 2020.2.1f1
Blender Version: 2.91
