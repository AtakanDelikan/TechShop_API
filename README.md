# 🛒 TechShop Backend – ASP.NET Core Web API

This is the backend for a full-featured e-commerce application built as a personal project. The application handles product management, order processing, CSV-based data import, and provides a custom analytics dashboard using real database queries.

> 👉 [Frontend Repository](https://github.com/AtakanDelikan/TechShop_frontend)

---

## 🔗 Live Demo

- 🌍 **Live Site**: [Hosted on Azure](https://tech-shop.azurewebsites.net/)
- 👤 **Demo Credentials**:
  - **Admin**: `admin / 12345`
  - Or register as a customer or admin

---

## 🧱 Tech Stack

| Layer          | Technology             |
| -------------- | ---------------------- |
| Backend        | ASP.NET Core Web API   |
| Database       | SQL Server             |
| ORM            | Entity Framework Core  |
| Authentication | ASP.NET Identity       |
| CSV Import     | CsvHelper              |
| Dev Tools      | Visual Studio, Swagger |

---

## 📂 Folder Structure

- `Controllers/` – API endpoints
- `Models/` – Entity definitions (e.g., Product, Category, Order)
- `Data/` – EF Core DbContext
- `Services/` – Helper services for controller
- `Utility/` – Some static definitions

---

## 🧩 Features

### 🛍️ Product & Category Management

- Hierarchical categories (`ParentCategoryId`)
- Dynamic attributes per category
- Products with rich metadata
- Bulk import from CSV (admin feature)

### 🔍 Filter & Search Products

- Dynamic Product Filtering – When browsing categories, filter options appear based on the specific attributes of that category (e.g., screen size, socket type)
- Search Functionality – Search products by name or keyword
- Scripts create category-specific mock products and export as CSV
- Ensures realistic field values (e.g. specs, prices)

### 📥 CSV Import Tools

- Admin-only endpoints:
  - `/importCategories`
  - `/importCategoryAttributes`
  - `/importProducts`
- Validates file size and type
- Uses CsvHelper for parsing
- Handles category hierarchy (parents before children)

### 📦 Orders & Checkout

- OrderHeader & OrderDetail models
- Tracks quantity, user, date, total
- Ratings + average rating update logic

### 📊 Analytics Dashboard

- Total revenue/order over time (line charts)
- Top-selling and top-grossing products
- Unique customer count and total item sold

### 🧪 Python Mock Data Generation

- Product data generated via custom Python scripts
- Scripts create category-specific mock products and export as CSV
- Ensures realistic field values (e.g. specs, prices)
- AI generated product images
