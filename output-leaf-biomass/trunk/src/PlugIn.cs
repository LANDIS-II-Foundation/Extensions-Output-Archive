//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Landis.Core;
using Edu.Wisc.Forest.Flel.Util;
using Landis.Library.LeafBiomassCohorts;
using Landis.SpatialModeling;
using Landis.Library.Metadata;

using System;
using System.Collections.Generic;
using System.IO;

namespace Landis.Extension.Output.LeafBiomass
{
    public class PlugIn
        : ExtensionMain
    {
        public static readonly string ExtensionName = "Output Leaf Biomass";
        public static readonly ExtensionType type = new ExtensionType("output");
        public static MetadataTable<SppBiomassLog> sppBiomassLog;
        public static MetadataTable<IndividualSppBiomassLog>[] individualBiomassLog;
        public static bool MakeMaps;

        private static ICore modelCore;
        private IEnumerable<ISpecies> selectedSpecies;
        private static string speciesMapNameTemplate;
        private IInputParameters parameters;

        //---------------------------------------------------------------------

        public PlugIn()
            : base("Output Leaf Biomass", type)
        {
        }

        //---------------------------------------------------------------------

        public static ICore ModelCore
        {
            get
            {
                return modelCore;
            }
        }

        //---------------------------------------------------------------------

        public override void LoadParameters(string dataFile, ICore mCore)
        {
            modelCore = mCore;
            InputParametersParser parser = new InputParametersParser();
            parameters = Landis.Data.Load<IInputParameters>(dataFile, parser);
        }
        //---------------------------------------------------------------------

        public override void Initialize()
        {

            Timestep = parameters.Timestep;
            SiteVars.Initialize();
            this.selectedSpecies = parameters.SelectedSpecies;
            speciesMapNameTemplate = parameters.SpeciesMaps;
            MakeMaps = parameters.MakeMaps;
            MetadataHandler.InitializeMetadata(parameters.Timestep, this.selectedSpecies, parameters.SpeciesMaps, modelCore);
            

        }

        //---------------------------------------------------------------------

        public override void Run()
        {
            WriteMapForAllSpecies();
            
            if (selectedSpecies != null)
            {
                if(MakeMaps)
                    WriteSpeciesMaps();
            }
            WriteLogFile();
        }

        //---------------------------------------------------------------------

        private void WriteSpeciesMaps()
        {
            foreach (ISpecies species in selectedSpecies)
            {
                string path = MapNames.ReplaceTemplateVars(speciesMapNameTemplate, species.Name, ModelCore.CurrentTime); //MakeSpeciesMapName(species.Name);
                PlugIn.ModelCore.UI.WriteLine("   Writing biomass map to {0} ...", path);
                using (IOutputRaster<IntPixel> outputRaster = modelCore.CreateRaster<IntPixel>(path, modelCore.Landscape.Dimensions))
                {
                    IntPixel pixel = outputRaster.BufferPixel;
                    foreach (Site site in ModelCore.Landscape.AllSites)
                    {
                        if (site.IsActive)
                            pixel.MapCode.Value = (int) ComputeBiomass(SiteVars.Cohorts[site][species]);
                        else
                            pixel.MapCode.Value = 0;
                        
                        outputRaster.WriteBufferPixel();
                    }
                }
            }

        }

        //---------------------------------------------------------------------

        private void WriteMapForAllSpecies()
        {
            // Biomass map for all species
            //string path = MakeSpeciesMapName("TotalBiomass");
            string path = MapNames.ReplaceTemplateVars(speciesMapNameTemplate, "TotalBiomass", ModelCore.CurrentTime);
            PlugIn.ModelCore.UI.WriteLine("   Writing TOTAL biomass map to {0} ...", path);
            using (IOutputRaster<IntPixel> outputRaster = modelCore.CreateRaster<IntPixel>(path, modelCore.Landscape.Dimensions))
            {
                IntPixel pixel = outputRaster.BufferPixel;
                foreach (Site site in ModelCore.Landscape.AllSites)
                {
                    if (site.IsActive)
                        pixel.MapCode.Value = (int) ComputeBiomass((ActiveSite) site);
                    else
                        pixel.MapCode.Value = 0;

                    outputRaster.WriteBufferPixel();
                }
            }
        }

        


        //---------------------------------------------------------------------

        private void WriteLogFile()
        {

            double[,] allSppEcos = new double[ModelCore.Ecoregions.Count, ModelCore.Species.Count];
            
            int[] activeSiteCount = new int[ModelCore.Ecoregions.Count];
            
            //UI.WriteLine("Next, reset all values to zero.");
            
            foreach (IEcoregion ecoregion in ModelCore.Ecoregions) 
            {
                int sppCnt = 0;
                foreach (ISpecies species in selectedSpecies) 
                {
                    allSppEcos[ecoregion.Index, sppCnt] = 0.0;
                    sppCnt++;
                }
                
                activeSiteCount[ecoregion.Index] = 0;
            }

            //UI.WriteLine("Next, accumulate data.");


            foreach (ActiveSite site in ModelCore.Landscape)
            {
                IEcoregion ecoregion = ModelCore.Ecoregion[site];
                
                int sppCnt = 0;
                foreach (ISpecies species in selectedSpecies) 
                {
                    allSppEcos[ecoregion.Index, sppCnt] += ComputeBiomass(SiteVars.Cohorts[site][species]);
                    sppCnt++;
                }
                
                activeSiteCount[ecoregion.Index]++;
            }
            
            foreach (IEcoregion ecoregion in ModelCore.Ecoregions)
            {
                sppBiomassLog.Clear();
                SppBiomassLog sbl = new SppBiomassLog();
                double[] sppBiomass = new double[modelCore.Species.Count];

                //int sppCnt = 0;
                foreach (ISpecies species in ModelCore.Species) 
                {
                    sppBiomass[species.Index] = allSppEcos[ecoregion.Index, species.Index] / (double)activeSiteCount[ecoregion.Index];
                }
                sbl.Time = ModelCore.CurrentTime;
                sbl.Ecoregion = ecoregion.Name;
                sbl.EcoregionIndex = ecoregion.Index;
                sbl.NumSites = activeSiteCount[ecoregion.Index];
                sbl.SppBiomass = sppBiomass;
                sppBiomassLog.AddObject(sbl);
                sppBiomassLog.WriteToFile();

                int selectSppCnt = 0;

                foreach (ISpecies species in selectedSpecies)
                {

                    double indSppBiomass = allSppEcos[ecoregion.Index, species.Index] / (double)activeSiteCount[ecoregion.Index];
                    
                    individualBiomassLog[selectSppCnt].Clear();
                    IndividualSppBiomassLog sb_individual = new IndividualSppBiomassLog();
                    sb_individual.Time = ModelCore.CurrentTime;
                    sb_individual.Ecoregion = ecoregion.Name;
                    sb_individual.EcoregionIndex = ecoregion.Index;
                    sb_individual.NumSites = activeSiteCount[ecoregion.Index];
                    sb_individual.SppBiomass = indSppBiomass;
                    individualBiomassLog[selectSppCnt].AddObject(sb_individual);
                    individualBiomassLog[selectSppCnt].WriteToFile();

                    selectSppCnt++;
                }

            }
        }
        ////---------------------------------------------------------------------

        //public static string MakeSpeciesMapName(string species)
        //{
        //    return SpeciesMapNames.ReplaceTemplateVars(speciesMapNameTemplate,
        //                                               species,
        //                                               ModelCore.CurrentTime);
        //}

    //--------------------------------------------------------------

        public static double ComputeBiomass(ISpeciesCohorts cohorts)
        {
            double total = 0.0;
            if (cohorts != null)
                foreach (ICohort cohort in cohorts)
                    total += (double) (cohort.LeafBiomass + cohort.WoodBiomass);
            return total;
        }

        //---------------------------------------------------------------------

        public static double ComputeBiomass(ActiveSite site) //ISiteCohorts cohorts)
        {
            double total = 0.0;
            if (SiteVars.Cohorts[site] != null)
                foreach (ISpeciesCohorts speciesCohorts in SiteVars.Cohorts[site])
                    total += ComputeBiomass(speciesCohorts);
            return total;
        }

    }
}