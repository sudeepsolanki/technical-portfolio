import { Category } from '../types';

interface CategoryListProps {
  categories: Category[];
  selectedCategoryId: number | null;
  onSelect: (categoryId: number | null) => void;
}

function CategoryList({ categories, selectedCategoryId, onSelect }: CategoryListProps) {
  return (
    <div className="category-list">
      <h3>Categories</h3>
      <ul>
        <li>
          <button
            className={`category-btn ${selectedCategoryId === null ? 'active' : ''}`}
            onClick={() => onSelect(null)}
          >
            All Products
          </button>
        </li>
        {categories.map((cat) => (
          <li key={cat.id}>
            <button
              className={`category-btn ${selectedCategoryId === cat.id ? 'active' : ''}`}
              onClick={() => onSelect(cat.id)}
            >
              {cat.name}
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default CategoryList;
