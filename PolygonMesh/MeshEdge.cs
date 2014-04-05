﻿/*
Copyright (c) 2014, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met: 

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies, 
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MatterHackers.PolygonMesh
{
    [DebuggerDisplay("ID = {Data.ID}")]
    public class MeshEdge
    {
        MetaData data = new MetaData();
        public MetaData Data { get { return data; } }

        public struct NextMeshEdgesFromEnds
        {
            MeshEdge nextMeshEdge0;
            MeshEdge nextMeshEdge1;
            public MeshEdge this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return nextMeshEdge0;
                    }
                    return nextMeshEdge1;
                }
                set
                {
                    if (index == 0)
                    {
                        nextMeshEdge0 = value;
                    }
                    else
                    {
                        nextMeshEdge1 = value;
                    }
                }
            }
        }

        public struct VertexOnEnds
        {
            Vertex vertex0;
            Vertex vertex1;
            public Vertex this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return vertex0;
                    }
                    return vertex1;
                }
                set
                {
                    if (index == 0)
                    {
                        vertex0 = value;
                    }
                    else
                    {
                        vertex1 = value;
                    }
                }
            }
        }

        public NextMeshEdgesFromEnds NextMeshEdgeFromEnd;
        public VertexOnEnds VertexOnEnd;

        public FaceEdge firstFaceEdge;

        public MeshEdge()
        {
            this.NextMeshEdgeFromEnd[0] = this; // start out with a circular reference to ourselves
            this.NextMeshEdgeFromEnd[1] = this; // start out with a circular reference to ourselves
        }

        public MeshEdge(Vertex vertex1, Vertex vertex2)
            : this()
        {
            this.VertexOnEnd[0] = vertex1;
            this.VertexOnEnd[1] = vertex2;

            AppendThisEdgeToEdgeLinksOfVertex(vertex1);
            AppendThisEdgeToEdgeLinksOfVertex(vertex2);
        }

        public void AddDebugInfo(StringBuilder totalDebug, int numTabs)
        {
            totalDebug.Append(new string('\t', numTabs) + String.Format("Vertex1: {0}\n", VertexOnEnd[0] != null ? VertexOnEnd[0].Data.ID.ToString() : "null"));
            totalDebug.Append(String.Format("Vertex1 Next MeshEdge: {0}\n", NextMeshEdgeFromEnd[0].Data.ID));

            totalDebug.Append(new string('\t', numTabs) + String.Format("Vertex2: {0}\n", VertexOnEnd[1] != null ? VertexOnEnd[1].Data.ID.ToString() : "null"));
            totalDebug.Append(String.Format("Vertex2 Next MeshEdge: {0}\n", NextMeshEdgeFromEnd[1].Data.ID));

            int firstFaceEdgeID = -1;
            if (firstFaceEdge != null)
            {
                firstFaceEdgeID = firstFaceEdge.Data.ID;
            }
            totalDebug.Append(new string('\t', numTabs) + String.Format("First FaceEdge: {0}\n", firstFaceEdgeID));
        }

        public MeshEdge GetNextMeshEdge(Vertex vertex)
        {
            int endVertecies = GetVertexEndIndex(vertex);
            return NextMeshEdgeFromEnd[endVertecies];
        }

        public int GetVertexEndIndex(Vertex vertexToGetIndexOf)
        {
            if (vertexToGetIndexOf == VertexOnEnd[0])
            {
                return 0;
            }
            else
            {
                if (vertexToGetIndexOf != VertexOnEnd[1])
                {
                    // if it is not the first one it must be the other one
                    throw new Exception("You must only ask to get the edge links for a MeshEdge that is linked to the given vertex.");
                }
                return 1;
            }
        }

        public int GetOpositeVertexEndIndex(Vertex vertexToNotGetIndexOf)
        {
            if (vertexToNotGetIndexOf == VertexOnEnd[0])
            {
                return 1;
            }
            else
            {
                if (vertexToNotGetIndexOf != VertexOnEnd[1])
                {
                    throw new Exception("You must only ask to get the edge links for a MeshEdge that is linked to the given vertex.");
                }
                return 0;
            }
        }

        void AppendThisEdgeToEdgeLinksOfVertex(Vertex vertexToAppendTo)
        {
            int endIndex = GetVertexEndIndex(vertexToAppendTo);

            if (vertexToAppendTo.firstMeshEdge == null)
            {
                // the vertex is not currently part of any edge
                // we are the only edge for this vertex so set its links all to this.
                vertexToAppendTo.firstMeshEdge = this;
                NextMeshEdgeFromEnd[endIndex] = this;
            }
            else // the vertex is already part of an edge (or many)
            {
                int endIndexOnFirstMeshEdge = vertexToAppendTo.firstMeshEdge.GetVertexEndIndex(vertexToAppendTo);

                // remember what the one that is there is poiting at
                MeshEdge vertexCurrentNext = vertexToAppendTo.firstMeshEdge.NextMeshEdgeFromEnd[endIndexOnFirstMeshEdge];

                // point the one that is there at us
                vertexToAppendTo.firstMeshEdge.NextMeshEdgeFromEnd[endIndexOnFirstMeshEdge] = this;

                // and point the ones that are already there at this.
                this.NextMeshEdgeFromEnd[endIndex] = vertexCurrentNext;
            }
        }

        internal bool IsConnectedTo(Vertex vertexToCheck)
        {
            if (VertexOnEnd[0] == vertexToCheck || VertexOnEnd[1] == vertexToCheck)
            {
                return true;
            }

            return false;
        }

        public int GetNumFacesSharingEdge()
        {
            int numFacesSharingEdge = 0;

            foreach (Face face in FacesSharingMeshEdgeIterator())
            {
                numFacesSharingEdge++;
            }

            return numFacesSharingEdge;
        }

        public IEnumerable<FaceEdge> FaceEdgesSharingMeshEdgeIterator()
        {
            FaceEdge curFaceEdge = this.firstFaceEdge;
            if (curFaceEdge != null)
            {
                do
                {
                    yield return curFaceEdge;

                    curFaceEdge = curFaceEdge.radialNextFaceEdge;
                } while (curFaceEdge != this.firstFaceEdge);
            }
        }

        public IEnumerable<Face> FacesSharingMeshEdgeIterator()
        {
            foreach (FaceEdge faceEdge in FaceEdgesSharingMeshEdgeIterator())
            {
                yield return faceEdge.containingFace;
            }
        }

        internal Vertex GetOppositeVertex(Vertex vertexToGetOppositeFor)
        {
            if (vertexToGetOppositeFor == VertexOnEnd[0])
            {
                return VertexOnEnd[1];
            }
            else
            {
                if (vertexToGetOppositeFor != VertexOnEnd[1])
                {
                    throw new Exception("You must only ask to get the opposite vertex on a MeshEdge that is linked to the given vertexToGetOppositeFor.");
                }
                return VertexOnEnd[0];
            }
        }

        internal FaceEdge GetFaceEdge(Face faceToFindFaceEdgeFor)
        {
            foreach (FaceEdge faceEdge in faceToFindFaceEdgeFor.FaceEdgeIterator())
            {
                if (faceEdge.containingFace == faceToFindFaceEdgeFor)
                {
                    return faceEdge;
                }
            }

            return null;
        }

        public void Validate()
        {
        }
    }
}
