# Event Visualizer
Visualize UnityEvent calls within the editor, both incoming and outgoing.

## In development
This package is still in development, it has not yet been tested at scale nor with all combinations of objects. Please report any issues you may find.

## How to use
Simply install the package and select any object that either has a UnityEvent component on it or is being called by one, it should shown either a yellow or pink arrow indicating the route of the event.  
To see a list of all objects and method names of the events go to `Window/Event Navigation`.  
The `Input` tab contains all incoming events from other objects that call on the selected object.  
The `Output` list contains all outgoing events that the selected objects calls

