"""Prove source/lightweight correspondence using Boy geometry anchors and graph refinement.

No family indices are input. Source variants may have different UV coordinates
and small neutral sculpt differences. Unique anchored graph colours must resolve
every vertex; a merely plausible nearest-point correspondence is insufficient.
"""
from collections import Counter
import numpy as np
from mathutils.kdtree import KDTree


def correspondence(source_points,source_faces,target_points,target_faces):
    assert len(source_points)==len(target_points)
    count=len(target_points);tree=KDTree(count)
    for i,p in enumerate(source_points):tree.insert(p,i)
    tree.balance();a=[0]*count;b=[0]*count;used=set();anchors=0;errors=[]
    # Boy's measured unchanged vertices differ by sub-micrometre FBX float noise.
    for i,p in enumerate(target_points):
        hits=tree.find_range(p,.000001)
        if len(hits)==1:
            _,j,error=hits[0]
            assert j not in used,'Non-bijective geometry anchor'
            used.add(j);a[j]=i+1;b[i]=i+1;anchors+=1;errors.append(error)
    def graph(faces):
        neighbours=[set() for _ in range(count)]
        for face in faces:
            for i,j in zip(face,face[1:]+face[:1]):neighbours[i].add(j);neighbours[j].add(i)
        return neighbours
    ag=graph(source_faces);bg=graph(target_faces)
    for iteration in range(32):
        ca=Counter(a);cb=Counter(b)
        assert ca==cb,'Anchored source/target topology differs'
        if all(v==1 for v in ca.values()):
            lookup={color:i for i,color in enumerate(a)}
            return np.array([lookup[color] for color in b]),dict(anchors=anchors,anchor_guard_mm=.001,anchor_max_error_mm=max(errors)*1000,refinement_iterations=iteration,unique_vertices=count)
        signatures={};next_colors=[]
        for colors,adjacency in [(a,ag),(b,bg)]:
            result=[]
            for i,color in enumerate(colors):
                signature=(color,tuple(sorted(colors[j] for j in adjacency[i])))
                if signature not in signatures:signatures[signature]=len(signatures)+1
                result.append(signatures[signature])
            next_colors.append(result)
        a,b=next_colors
    raise AssertionError('Boy source-to-lightweight correspondence remains ambiguous; stop')
