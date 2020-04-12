To calculate the observed portion of a body, we use a polyhedron with 100 surfaces
to map the globe. We then use the normals of those surfaces to determine if the
surface is visible or not.

This was downloaded from https://www.chiark.greenend.org.uk/~sgtatham/polyhedra/
and kept here as a copy for reference.

To generate the data used:

python mkpoints.py 100 | python nfaces.py | python cleanpoly.py > 100polygons.txt
python drawnet.py 100polygons.txt > net100.ps
python drawpoly.py 100polygons.txt > poly100.ps

to generate source from the normals:

grep normal 100polygons.txt | sed -e "s/normal //" -e "s/ /, /g" -e "s/^/surfaces.add(new Surface(\"/" -e "s/,/\",/" -e "s/$/));/" > code.cs
