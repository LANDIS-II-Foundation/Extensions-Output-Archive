//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Landis.Core;
using System.Collections.Generic;

namespace Landis.Extension.Output.BiomassPnET
{
	/// <summary>
	/// The parameters for the plug-in.
	/// </summary>
	public interface IInputParameters
	{
		/// <summary>
		/// Timestep (years)
		/// </summary>
		int Timestep
		{
			get;set;
		}

		//---------------------------------------------------------------------

		/// <summary>
		/// Collection of species for which biomass maps are generated.
		/// </summary>
		/// <remarks>
		/// null if no species are selected.
		/// </remarks>
		IEnumerable<ISpecies> SelectedSpecies
		{
			get;set;
		}

		//---------------------------------------------------------------------

        string SpeciesEstMapNames
        {
            get;set;
        }
        string WaterMapNameTemplate
        {
            get;set;
        }
        string AnnualTranspirationMapNames
        {
            get;set;
        }

        string SubCanopyPARMapNames
        {
            get;set;
        }
        string SpeciesLAIMapNames
        {
            get;
            set;
        }

		/// <summary>
		/// Template for the filenames for species biomass maps.
		/// </summary>
		/// <remarks>
		/// null if no species are selected.
		/// </remarks>
		string SpeciesBiomMapNames
		{
			get;set;
		}

        string BelowgroundMapNames
		{
			get;set;
		}
		//---------------------------------------------------------------------

		/// <summary>
		/// Dead pools for which biomass maps are generated.
		/// </summary>
		//SelectedDeadPools SelectedPools
		//{
		//	get;
		//}
        string SelectedPools
        {
            get;set;
        }
		//---------------------------------------------------------------------

		/// <summary>
		/// Template for the filenames for dead-pool biomass maps.
		/// </summary>
		/// <remarks>
		/// null if no pools are selected.
		/// </remarks>
		string PoolMapNames
		{
			get;
		}
        bool MakeTable { get; }
    }
}