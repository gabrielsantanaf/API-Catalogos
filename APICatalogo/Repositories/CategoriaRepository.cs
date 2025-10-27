using APICatalogo.Context;
using APICatalogo.Models;
using APICatalogo.Pagination;
using Microsoft.EntityFrameworkCore;

namespace APICatalogo.Repositories
{
    public class CategoriaRepository : Repository<Categoria>, ICategoriaRepository
    {
        private readonly AppDbContext _context;

        public CategoriaRepository(AppDbContext context) : base(context)
        {
        }

        public PagedList<Categoria> GetCategorias(CategoriasParameters categoriasParams)
        {
            var categorias = GetAll().OrderBy(p => p.CategoriaId).AsQueryable();

            var categoriasOrdenados = PagedList<Categoria>.ToPagedList(categorias, categoriasParams.PageNumber, categoriasParams.PageSize);

            return categoriasOrdenados;
        }

        public PagedList<Categoria> GetCategoriasFiltroNome(CategoriasFiltroNome categoriasParams)
        {
            var categorias = GetAll().AsQueryable();

            if (!string.IsNullOrEmpty(categoriasParams.Nome))
            {
                categorias = categorias.Where(c => c.Nome.Contains(categoriasParams.Nome));
            }

            var categoriasFiltradas = PagedList<Categoria>.ToPagedList(categorias, categoriasParams.PageNumber, categoriasParams.PageSize);
            
            return categoriasFiltradas;
        }
    }
}
