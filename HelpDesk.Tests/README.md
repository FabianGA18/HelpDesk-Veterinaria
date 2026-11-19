# Pruebas automatizadas – HelpDesk Veterinaria

Proyecto xUnit con pruebas unitarias, de servicios y de integración (la aplicación completa en memoria).
No necesita SQL Server: usa SQLite en memoria y una carpeta temporal para las evidencias.

## Cómo ejecutarlas

Desde la raíz del repositorio:

```bash
dotnet test HelpDesk.Tests
```

En Visual Studio: menú **Prueba → Explorador de pruebas → Ejecutar todas**.

Para correr solo las pruebas de una historia o requisito (los «Trait»):

```bash
dotnet test HelpDesk.Tests --filter "HU=HU-03"
dotnet test HelpDesk.Tests --filter "RF=RF-12"
dotnet test HelpDesk.Tests --filter "RNF=RNF-02"
```

## Organización

| Carpeta | Qué prueba |
|---|---|
| `Unitarias/` | Lógica pura: flujo de estados (RF-12), validación de archivos (RNF-06), formatos. |
| `Servicios/` | Reglas de negocio de `TiqueteService`, `UsuarioService` y `CategoriaService` contra la base de datos. |
| `Integracion/` | La app web completa: login y permisos por rol, portal del solicitante, soporte, administración y evidencias. |
| `Infraestructura/` | Utilidades: servidor de prueba (`AppFactory`), cliente web con login y token (`ClienteWeb`), datos semilla. |

Cada prueba está etiquetada con la historia (`HU`), el requisito funcional (`RF`) o el no funcional (`RNF`)
que verifica, para la matriz de trazabilidad.

## Notas

- Las migraciones del proyecto son de SQL Server. En las pruebas el esquema se crea desde el modelo
  y las migraciones se marcan como aplicadas (ver `BaseDatosPrueba`).
- Los usuarios de prueba son los de `SeedData` (contraseña `Vet12345!`).
