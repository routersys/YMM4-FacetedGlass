using System.ComponentModel.DataAnnotations;

namespace FacetedGlass
{
    public enum FacetedGlassMode
    {
        [Display(Name = nameof(Texts.ModeVoronoi), Description = nameof(Texts.ModeVoronoiDescription), ResourceType = typeof(Texts))]
        Voronoi = 0,

        [Display(Name = nameof(Texts.ModeTriangle), Description = nameof(Texts.ModeTriangleDescription), ResourceType = typeof(Texts))]
        Triangle = 1,
    }
}
