using Catalog.API.Exceptions;
using Catalog.API.Products.CreateProduct;

namespace Catalog.API.Products.UpdateProduct
{
    
    public record UpdateProductCommand(Guid Id, string Name, List<string> Category, string Description, string ImageFile, decimal Price) : ICommand<UpdateProductResult>;
    public record UpdateProductResult(bool IsSuccess);

    public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Product ID is required");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Product Name is required")
                                .Length(2, 100).WithMessage("Product Name must be between 2 and 100 characters");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero");
        }
    }
    internal class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, UpdateProductResult>
    {
        private readonly IDocumentSession _session;
        private readonly ILogger<UpdateProductCommandHandler> _logger;
        public UpdateProductCommandHandler(IDocumentSession session, ILogger<UpdateProductCommandHandler> logger)
        {
            _session = session;
            _logger = logger;
        }
        public async Task<UpdateProductResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("UpdateProductHandler .Handle called with {@Command}", command);
            var product = await _session.LoadAsync<Product>(command.Id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product with Id: {Id} not found", command.Id);
                throw new ProductNotFoundException();
            }
            product.Name = command.Name;
            product.Category = command.Category;
            product.Description = command.Description;
            product.ImageFile = command.ImageFile;
            product.Price = command.Price;
            _session.Update(product);
            await _session.SaveChangesAsync(cancellationToken);
            return new UpdateProductResult(true);
        }
    }
}
