using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.HPSF;
using SoulsFormats;

namespace SoulsParamsConverter.Commands
{
    public class FlverPropertiesUpdater
    {
        public static void Run(bool? backfaceCulling, string source)
        {
            var archives = source.Split(";");
            foreach (var archive in archives)
            {
                var bnd = BND4.Read(archive);
                var flvers = bnd.Files.Where(file => file.Name.EndsWith(".flver"));

                foreach (var flver in flvers)
                {
                    Console.WriteLine($"Updating {flver.Name}");

                    var flverModel = FLVER2.Read(flver.Bytes);
                    
                    foreach (var mesh in flverModel.Meshes)
                    {
                        foreach (var faceSet in mesh.FaceSets)
                        {
                            if (backfaceCulling != null)
                            {
                                faceSet.CullBackfaces = backfaceCulling.Value;
                            }
                        }
                    }

                    flver.Bytes = flverModel.Write();
                }

                bnd.Write(archive);
            }
        }
    }
}