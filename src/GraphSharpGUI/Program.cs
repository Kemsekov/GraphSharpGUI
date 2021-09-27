/*
* шейдер для окошка и compute shader
* будет только одна текстурка которая ресайзится под размер окна
* эта текстурка будет обрабатываться в compute shader и рисоваться в window shader.
* 
* Как рисовать граф
* загружаем в видеокарту следущее:
* массив из [индекс массива - номер Нода : координата, кол-во детей, номер элемента в массиве 2 с которого идут его дети] - тут каждая нода уникальна
* мап из [Нода : child ноды] - тут ноды могут( и будут) повторяться, отсортированы но номеру ноды, как сами ноды так и их дети
* координаты нормализованные (x>0.0 && x<1.0)
*/

//шаг первый создать окно, текстуру в окне и ресайзить её в зависимости от ресайза окна


using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using AutoPipeline;
using GraphSharp;
using GraphSharpGUI;
using SampleBase;

void TransformNodes(NodeBase[] nodes, out int[] nodesId, out NodeInfo[] nodesInfo,Color nodes_color)
{
    var _nodesInfo = new NodeInfo[nodes.Length];
    int count = 0;
    var rand = new Random(8);

    nodesId = nodes.OrderBy(v => v.Id).SelectMany(
        v =>
        {
            _nodesInfo[v.Id] = new NodeInfo(
                v.Id,
                new((float)(rand.NextDouble()), (float)(rand.NextDouble())),
                v.Childs.Count,
                count,
                nodes_color.ToArgb());
            count += v.Childs.Count;
            return v.Childs.OrderBy(c => c.Id);
        }).Select(v => v.Id).ToArray();
    nodesInfo = _nodesInfo;
}

var nodes = NodeGraphFactory.CreateRandomConnected<Node>(1000,1,3).ToArray();

TransformNodes(nodes, out int[] nodesId, out NodeInfo[] nodesInfo,Color.FromArgb(0,0,0,0));


var window = new VeldridStartupWindow("Compute Texture");
var manager = new DrawUnitManager(window);
//add draw units to manager

var create_texture = new CreateResizableTexture(manager, File.ReadAllBytes("Shaders/FillImage.comp"));

var drawNodes = new DrawNodesOnTexture(create_texture, File.ReadAllBytes("Shaders/Compute.comp"), nodesInfo, nodesId);

//var copyNodes = new CopyBuffer(manager, () => drawNodes.NodesInfoBuffer, Unsafe.SizeOf<NodeInfo>() * nodesInfo.Length);
var drawTexture = new DrawTexture(create_texture,File.ReadAllBytes("Shaders/Vertex.glsl"),File.ReadAllBytes("Shaders/Fragment.glsl"));


Graph g = new Graph(nodes);

Dictionary<int,int> repeats = new();
ActionVesitor vesitor = new ActionVesitor(node=>{
        if(nodesInfo[node.Id].NodeId==node.Id){
            repeats[node.Id]+=1;
            if(repeats[node.Id]<3)
                nodesInfo[node.Id].RGBAColor = Color.FromArgb(255,175,0,255).ToArgb();
        }
});
g.AddVesitor(vesitor);

using var timer = new Timer(
    s=>{
        lock(g){
            for(int i = 0;i<nodesInfo.Length;i++){
                if(nodesInfo[i].RGBAColor==Color.FromArgb(204,54,0,255).ToArgb()){
                    nodesInfo[i].RGBAColor=Color.FromArgb(255,190,90,255).ToArgb();
                }
                else
                if(nodesInfo[i].RGBAColor==Color.FromArgb(255,175,0,255).ToArgb()){
                    nodesInfo[i].RGBAColor=Color.FromArgb(204,54,0,255).ToArgb();
                }
                else{
                    repeats[i] = 0;
                    nodesInfo[i].RGBAColor=Color.FromArgb(0,0,0,0).ToArgb();
                }
            }
            g.Step();
            drawNodes.UpdateResources();
        }
    },
    null,
    1000,
    100
);

window.Run();

