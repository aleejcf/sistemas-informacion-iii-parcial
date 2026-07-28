# Investigación: Mejores técnicas de estructura Backend y Frontend
### Cómo desarrollan software los profesionales a nivel mundial — y cómo aplicarlo a PARKO

---

## 1. Arquitectura de software (la estructura del backend)

### La regla de oro: separación en capas

Los equipos profesionales **nunca mezclan** la interfaz con la lógica y los datos. La arquitectura estándar en .NET es la **Arquitectura Limpia (Clean Architecture)** de Robert C. Martin ("Uncle Bob"), con estas capas:

| Capa | Qué contiene | En PARKO |
|------|--------------|----------|
| **Dominio (Models)** | Las entidades y reglas del negocio | `Models\Usuario.vb` |
| **Aplicación (Services)** | Los casos de uso: registrar, autenticar, cobrar | `Services\AuthService.vb`, `MovimientoService.vb` |
| **Infraestructura (Data)** | El acceso a la base de datos | `Services\Db.vb` |
| **Presentación (Views)** | Ventanas, páginas, controles | `Views\` |

**Regla de dependencia**: las capas internas (Dominio) *nunca* conocen a las externas (Presentación). La interfaz puede cambiar de WPF a web sin tocar la lógica — exactamente lo que hicimos al crear ParkoForms reutilizando los mismos Services. Esa reutilización fue posible **gracias** a esta arquitectura.

### Patrones de acceso a datos

- **Repository Pattern**: una clase por entidad que encapsula todo su SQL (nuestros `ClienteService`, `ParqueaderoService`…). El resto de la app no sabe si los datos vienen de SQL Server, SQLite o un archivo.
- **Dependency Injection (DI)**: en vez de que cada clase cree sus dependencias, se las "inyectan" desde afuera. Facilita las pruebas y el cambio de implementaciones. En .NET se usa `Microsoft.Extensions.DependencyInjection`. *Nivel siguiente para PARKO*: convertir los servicios `Shared` en instancias inyectadas.
- **ORM (Entity Framework Core)**: los profesionales suelen mapear tablas a clases automáticamente en lugar de escribir SQL a mano. Regla: el ORM se queda en la capa de infraestructura, nunca se filtra al dominio.

---

## 2. Frontend: patrones de interfaz profesional

### MVVM — el patrón estándar de WPF

**Model-View-ViewModel** es el patrón para el que WPF fue diseñado:

- **View** (XAML): solo apariencia, sin lógica
- **ViewModel**: el estado de la pantalla y los comandos; se conecta a la View con *data binding*
- **Model**: los datos del negocio

Claves profesionales: `ObservableCollection` para listas que se actualizan solas, `ICommand` para los botones (en vez de eventos Click), el patrón *Messenger* para comunicar ventanas sin acoplarlas, y **async/await** para que la interfaz nunca se congele (ya lo aplicamos en el botón "Ingresando…" del login).

*Nivel siguiente para PARKO*: migrar las páginas de code-behind a ViewModels con `CommunityToolkit.Mvvm` (el toolkit oficial de Microsoft).

### Design System — consistencia visual

Las empresas definen un **sistema de diseño**: paleta de colores limitada, tipografía, espaciados en múltiplos de 8px, y componentes reutilizables. PARKO ya tiene el suyo en `Application.xaml`: paleta de marca (#0A2540, #00E676), estilos `BotonPrimario`, `CajaTexto`, `Tarjeta` — eso ES un design system.

### Microinteracciones

Animaciones pequeñas y funcionales que dan retroalimentación inmediata: botones que reaccionan al hover, sacudida al fallar, estados de carga, transiciones de entrada. Reglas profesionales: duraciones cortas (100–400 ms), consistentes en toda la app, y **siempre con propósito** (confirmar una acción, guiar la atención) — nunca decoración porque sí. PARKO ya aplica: entrada en cascada, sacudida de error, "Ingresando…", brillo del logo, escala al hover.

### Accesibilidad

- Contraste suficiente entre texto y fondo
- Navegación completa con teclado (Tab en orden lógico, Enter para confirmar)
- Textos no demasiado pequeños (por eso subimos la fuente de las cajas)
- Evitar transmitir información solo con color

---

## 3. Principios de código limpio

### SOLID — los 5 principios que todo profesional domina

| Letra | Principio | En cristiano |
|-------|-----------|--------------|
| **S** | Single Responsibility | Cada clase hace UNA sola cosa (por eso `AuthService` no dibuja ventanas) |
| **O** | Open/Closed | Abierto a extensión, cerrado a modificación: agregar sin romper lo existente |
| **L** | Liskov Substitution | Una subclase debe poder usarse donde se usa la clase base |
| **I** | Interface Segregation | Interfaces pequeñas y específicas, no gigantes |
| **D** | Dependency Inversion | Depender de abstracciones (interfaces), no de implementaciones concretas |

### Otros principios clave

- **DRY** (Don't Repeat Yourself): si copias y pegas código, extráelo a un método/control común (nuestro `CajaClaveVisible` es DRY aplicado)
- **KISS** (Keep It Simple): la solución más simple que funcione; la complejidad se paga cara
- **YAGNI** (You Aren't Gonna Need It): no construyas cosas "por si acaso"
- **Nombres que se explican solos**: `RegistrarSalida()` en vez de `Proc3()`

---

## 4. Seguridad (estándar OWASP)

OWASP es la referencia mundial en seguridad de aplicaciones. Lo esencial:

| Práctica | Estado en PARKO |
|----------|:---:|
| Contraseñas **hasheadas** (BCrypt/Argon2), jamás en texto plano | ✅ BCrypt factor 11 |
| Consultas **parametrizadas** (nunca concatenar SQL → inyección SQL) | ✅ En todos los servicios |
| **Bloqueo** tras intentos fallidos | ✅ 3 intentos → 30 s |
| Validación de entradas (email, longitud, formato) | ✅ `Validador.vb` |
| Roles y permisos (menor privilegio) | ✅ Administrador/Operador |
| Respuestas de seguridad también hasheadas | ✅ |
| MFA (segundo factor) | Futuro: código por correo |
| Mensajes de error que no revelan información interna | Mejorable: no mostrar `ex.Message` crudo al usuario final |

---

## 5. Base de datos profesional

- **Normalización (hasta 3FN)**: cada dato vive en un solo lugar; tablas relacionadas por llaves foráneas. La BD `parqueadero` ya está normalizada con FKs.
- **Índices**: en columnas usadas en WHERE/JOIN/ORDER BY. *Mejora para PARKO*: índice en `movimiento.placa` y `movimiento.fecha_salida` (las buscamos constantemente).
- **Procedimientos almacenados**: encapsulan lógica en el servidor, mejoran seguridad y rendimiento. El profesor probablemente los valore.
- **Tipos de dato correctos**: `DECIMAL` para dinero (nunca FLOAT), `DATETIME` para fechas — ya lo hacemos.
- **Documentar el esquema** y guardar los scripts en el repositorio (nuestro `Scripts\01_tablas_sistema.sql`).

---

## 6. Prácticas de equipos profesionales

1. **Control de versiones (Git)**: commits pequeños y frecuentes con mensajes claros, ramas por funcionalidad, y la rama principal siempre estable. *Recomendación fuerte: inicializar un repositorio Git para PARKO — es la práctica #1 de la industria.*
2. **Code review**: nadie fusiona código sin que otro lo revise (Pull Requests en GitHub).
3. **Pruebas automatizadas**: pruebas unitarias de la lógica (ej. verificar que `RegistrarSalida` cobra bien la hora o fracción). En .NET: xUnit/NUnit. Nuestra arquitectura en capas hace esto posible — los Services se pueden probar sin abrir ventanas.
4. **CI/CD**: cada cambio se compila y prueba automáticamente (GitHub Actions). Si algo se rompe, se detecta en minutos.
5. **Documentación**: un README que explique qué hace el sistema, cómo instalarlo y cómo correrlo.

---

## 7. Hoja de ruta para PARKO

**Ya lo tienes (nivel profesional para un parcial):** arquitectura en capas, BCrypt, SQL parametrizado, roles, design system, microinteracciones, async en login, diálogos propios, control reutilizable.

**Siguiente paso (impacto alto, esfuerzo bajo):**
1. Repositorio Git + README
2. Índices en `movimiento` y un par de procedimientos almacenados
3. No mostrar `ex.Message` al usuario (mensaje amigable + log interno)

**Futuro (para seguir creciendo como dev):**
4. MVVM con CommunityToolkit.Mvvm
5. Inyección de dependencias
6. Pruebas unitarias de los Services con xUnit
7. Entity Framework Core en lugar de SQL manual
8. GitHub Actions para compilar en cada push

---

## Fuentes consultadas

- [Clean Architecture in .NET — Milan Jovanović](https://milanjovanovic.tech/blog/clean-architecture-dotnet)
- [Implementing Clean Architecture in .NET: 2026 Best Practices](https://www.gatistavamsoftech.com/implementing-clean-architecture-in-net-2026-best-practices/)
- [Clean Architecture in .NET: Step-by-Step Guide 2026](https://niotechone.com/blog/clean-architecture-in-dotnet-a-step-by-step-guide-for-2026/)
- [10 WPF Best Practices — PostSharp](https://blog.postsharp.net/wpf-best-practices-2024)
- [Recommendations for MVVM/XAML apps — Rico Suter](https://blog.rsuter.com/recommendations-best-practices-implementing-mvvm-xaml-net-applications/)
- [Modern WPF Development: MVVM and Prism — eInfochips](https://www.einfochips.com/blog/modern-wpf-development-leveraging-mvvm-and-prism-for-enterprise-app/)
- [Dependency Injection & SOLID — DotNetCurry](https://www.dotnetcurry.com/software-gardening/1284/dependency-injection-solid-principles)
- [Design Patterns: Dependency Injection — Stackify](https://stackify.com/dependency-injection/)
- [OWASP Secure Coding Practices Guide 2026](https://www.appsecmaster.net/blog/owasp-secure-coding-practices-guide/)
- [Password Storage and Hashing — OWASP Cheat Sheet](https://deepwiki.com/OWASP/CheatSheetSeries/3.1-password-storage-and-hashing)
- [Microinteractions in UI/UX — Mobisoft](https://mobisoftinfotech.com/resources/blog/microinteractions-ui-ux-design-trends-examples)
- [11 UI Design Best Practices 2026 — UX Playbook](https://uxplaybook.org/articles/ui-fundamentals-best-practices-for-ux-designers)
- [Best Practices for Database Design in SQL — Medium](https://medium.com/learning-sql/best-practices-for-database-design-in-sql-d421039a3590)
- [11 SQL Server Index Best Practices — Quest](https://blog.quest.com/11-sql-server-index-best-practices-for-improved-performance-tuning/)
- [6 Software Engineering Best Practices 2026 — Zencoder](https://zencoder.ai/blog/software-engineering-best-practices)
- [Code Review Best Practices — daily.dev](https://daily.dev/blog/software-engineering-best-practices-for-code-review/)
- [Git & Version Control Best Practices — DEV](https://dev.to/aneeqakhan/best-practices-for-git-and-version-control-588m)
