# Byte Array Accessors


### Todo:

##### ListSegment < T >

create a  equivalent to ArraySegment < T >


##### generic accessor

for vertex accessors, all accessors must implement a Vector4 accessor,
which might be considered as the "object" accessor

##### safe implementations

probably a net35 target platform with alternative, slower, accessors


##### basic mesh builder

An ideal mesh builder would work like this:

struct myVertex1
{
[MeshBuilder.Position]
public system.numerics.Vector3 Position;

[MeshBuilder.Normal]
public system.numerics.Vector3 Normal;

}

mesh = new MeshBuilder<myVertex1>(float,ushort16);

var a = myVertex1();
var b = myVertex1();
var c = myVertex1();

mesh.AddTriangle(a,b,c);


