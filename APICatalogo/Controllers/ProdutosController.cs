using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using APICatalogo.Context;
using APICatalogo.Models;
using System.Reflection.Metadata.Ecma335;
using Microsoft.EntityFrameworkCore;
using APICatalogo.Repositories;

namespace APICatalogo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutosController : ControllerBase
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IConfiguration _configuration;
        private readonly IRepository<Produto> _repository;
        public ProdutosController(IRepository<Produto> repository, IProdutoRepository produtoRepository, IConfiguration configuration)
        {
            _produtoRepository = produtoRepository;
            _repository = repository;
            _configuration = configuration;
        }

        [HttpGet("produtos/{id}")]
        public ActionResult <IEnumerable<Produto>> GetProdutoCategoria(int id)
        {
            var produtos = _produtoRepository.GetProdutosPorCategoria(id);

            if (produtos is null)
                return NotFound();

            return Ok(produtos);
        }

        [HttpGet("LerArquivoConfiguration")]
        public string GetValores()
        {
            var valor1 = _configuration["chave1"];
            var valor2 = _configuration["chave2"];

            var secao1 = _configuration["secao1:chave2"];

            return $"Chave1 = {valor1} \nChave2 = {valor2} \nSeção1 => Chave2 = {secao1}";
        }

        [HttpGet]
        public ActionResult<IEnumerable<Produto>> Get()
        {

            var produtos = _repository.GetAll();
            if (produtos is null)
            {
                return NotFound("Produtos não encontrados...");
            }
            return Ok(produtos);


        }

        [HttpGet("{id:int}", Name = "ObterProduto")]
        public ActionResult<Produto> Get(int id)
        {

            var produto = _repository.Get(c => c.ProdutoId == id);  
            if (produto is null)
            {
                return NotFound("Produto não encontrado...");
            }
            return Ok(produto);

        }

        [HttpPost]
        public ActionResult Post(Produto produto)
        {

            if (produto is null)
            {
                return BadRequest();
            }

            var novoProduto = _repository.Create(produto);

            return new CreatedAtRouteResult("ObterProduto",
                new { id = novoProduto.ProdutoId }, novoProduto);


        }

        [HttpPut("{id:int}")]
        public ActionResult Put(int id, Produto produto)
        {

            if (id != produto.ProdutoId)
            {
                return BadRequest();
            }

            var produtoAtualizado = _repository.Update(produto);
            return Ok(produtoAtualizado);

        }

        [HttpDelete("{id:int}")]
        public ActionResult Delete(int id)
        {
            var produto = _repository.Get(c => c.ProdutoId == id);
            if (produto is null)
            {
                return NotFound("Produto não encontrado...");
            }
            var produtoDeletado = _repository.Delete(produto);
            return Ok(produtoDeletado);
        }
    }
}






