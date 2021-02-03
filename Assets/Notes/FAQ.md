# Here lists some general questions and tips

## Forbidden / Must attention
1. necver use update / fixedUpdate to change States, use Event instead! in case you really need this, backup your current version in case you need to roll back!
2. abstract class should have no Lifecircle functions, write them as virtual and override in end class!
## LifeCycle
### Awake
1. Find Objects
   
### OnEnable
1. init settings when needed

### Start
1. init properties and fields

## OnTriggerEnter
1. follow the "who is moving" principle. write the function in whose script
2. if both are moving, write the owned part in each script.
3. in complex situation, use state machine instead