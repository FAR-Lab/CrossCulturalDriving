/* Notes for CMU AV Meeting

The current implementation has the approach of "appear to be self driving"
rather than training a neural network to handle everything

This is achieved by the combination of:
 * PID controller for path following
 * state machine to control vehicle speed
 * basic obstacle avoidance


Meeting note:
Want realistic traffic
Interactions:
 * Play music  --  (car misinterprets the command?
 * Change driving behavior
 * Dinner reservation -- appears to help but fails

Focus on internal interaction 
Length of the driver: 10 mins
Repeatedly drive in same city block: participant does secondary task so wont notice
Collision warning: working collision avoidance system but sometimes show false positive

Bare minimum - a self driving car around the block that runs for 20 mins
 * Accomodate long study run: Ability to stop/slow down the car

Ped and other cars
Light to medium traffic 
Good to have some automatic interactions among entities

Timeline: Scenarios by early spring

*/