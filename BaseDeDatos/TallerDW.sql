
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE master;
GO

IF DB_ID('TallerDW') IS NULL
    CREATE DATABASE TallerDW;
GO

USE TallerDW;
GO

IF OBJECT_ID('dbo.Roles') IS NOT NULL
BEGIN
    RAISERROR('La base de datos TallerDW ya contiene tablas. Si desea reinstalarla, descomente el bloque DROP DATABASE al inicio del script.', 16, 1);
    SET NOEXEC ON;
END
GO

/* =====================================================================
   SEGURIDAD
   ===================================================================== */

CREATE TABLE dbo.Roles (
    RolId           INT IDENTITY(1,1) CONSTRAINT PK_Roles PRIMARY KEY,
    Nombre          NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Roles_Nombre UNIQUE,
    Descripcion     NVARCHAR(200) NULL,
    EsSistema       BIT NOT NULL CONSTRAINT DF_Roles_EsSistema DEFAULT (0),
    Activo          BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1)
);
GO

CREATE TABLE dbo.Modulos (
    ModuloId        INT IDENTITY(1,1) CONSTRAINT PK_Modulos PRIMARY KEY,
    Codigo          NVARCHAR(30)  NOT NULL CONSTRAINT UQ_Modulos_Codigo UNIQUE,
    Nombre          NVARCHAR(80)  NOT NULL,
    Descripcion     NVARCHAR(200) NULL,
    Grupo           NVARCHAR(30)  NOT NULL,
    Icono           NVARCHAR(50)  NOT NULL,
    Orden           INT NOT NULL
);
GO

CREATE TABLE dbo.RolPermisos (
    RolId           INT NOT NULL CONSTRAINT FK_RolPermisos_Roles REFERENCES dbo.Roles(RolId) ON DELETE CASCADE,
    ModuloId        INT NOT NULL CONSTRAINT FK_RolPermisos_Modulos REFERENCES dbo.Modulos(ModuloId),
    PuedeVer        BIT NOT NULL CONSTRAINT DF_RolPermisos_Ver DEFAULT (0),
    PuedeCrear      BIT NOT NULL CONSTRAINT DF_RolPermisos_Crear DEFAULT (0),
    PuedeEditar     BIT NOT NULL CONSTRAINT DF_RolPermisos_Editar DEFAULT (0),
    PuedeEliminar   BIT NOT NULL CONSTRAINT DF_RolPermisos_Eliminar DEFAULT (0),
    CONSTRAINT PK_RolPermisos PRIMARY KEY (RolId, ModuloId)
);
GO

CREATE TABLE dbo.Usuarios (
    UsuarioId       INT IDENTITY(1,1) CONSTRAINT PK_Usuarios PRIMARY KEY,
    NombreCompleto  NVARCHAR(120) NOT NULL,
    NombreUsuario   NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE,
    Correo          NVARCHAR(120) NULL,
    Telefono        NVARCHAR(20)  NULL,
    PasswordHash    NVARCHAR(200) NOT NULL,
    RolId           INT NOT NULL CONSTRAINT FK_Usuarios_Roles REFERENCES dbo.Roles(RolId),
    Activo          BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
    FechaCreacion   DATETIME2(0) NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT (SYSDATETIME()),
    UltimoAcceso    DATETIME2(0) NULL
);
GO

CREATE TABLE dbo.Parametros (
    ParametroId     INT IDENTITY(1,1) CONSTRAINT PK_Parametros PRIMARY KEY,
    Clave           NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Parametros_Clave UNIQUE,
    Valor           NVARCHAR(250) NOT NULL,
    Descripcion     NVARCHAR(200) NULL,
    Tipo            NVARCHAR(10)  NOT NULL CONSTRAINT DF_Parametros_Tipo DEFAULT ('TEXTO')
                    CONSTRAINT CK_Parametros_Tipo CHECK (Tipo IN ('TEXTO','NUMERO'))
);
GO

CREATE TABLE dbo.Bitacora (
    BitacoraId      BIGINT IDENTITY(1,1) CONSTRAINT PK_Bitacora PRIMARY KEY,
    UsuarioId       INT NULL CONSTRAINT FK_Bitacora_Usuarios REFERENCES dbo.Usuarios(UsuarioId),
    NombreUsuario   NVARCHAR(50)  NULL,
    Modulo          NVARCHAR(30)  NOT NULL,
    Accion          NVARCHAR(20)  NOT NULL,
    Detalle         NVARCHAR(500) NULL,
    Ip              NVARCHAR(50)  NULL,
    Fecha           DATETIME2(0) NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT (SYSDATETIME())
);
GO
CREATE INDEX IX_Bitacora_Fecha ON dbo.Bitacora(Fecha DESC);
GO

/* =====================================================================
   CLIENTES Y VEHÍCULOS
   ===================================================================== */

CREATE TABLE dbo.Clientes (
    ClienteId       INT IDENTITY(1,1) CONSTRAINT PK_Clientes PRIMARY KEY,
    Nombres         NVARCHAR(80)  NOT NULL,
    Apellidos       NVARCHAR(80)  NOT NULL,
    Nit             NVARCHAR(20)  NOT NULL CONSTRAINT DF_Clientes_Nit DEFAULT ('CF'),
    Dpi             NVARCHAR(20)  NULL,
    Telefono        NVARCHAR(20)  NOT NULL,
    Correo          NVARCHAR(120) NULL,
    Direccion       NVARCHAR(250) NULL,
    Activo          BIT NOT NULL CONSTRAINT DF_Clientes_Activo DEFAULT (1),
    FechaRegistro   DATETIME2(0) NOT NULL CONSTRAINT DF_Clientes_FechaRegistro DEFAULT (SYSDATETIME())
);
GO
CREATE UNIQUE INDEX UX_Clientes_Dpi ON dbo.Clientes(Dpi) WHERE Dpi IS NOT NULL;
GO

CREATE TABLE dbo.Vehiculos (
    VehiculoId      INT IDENTITY(1,1) CONSTRAINT PK_Vehiculos PRIMARY KEY,
    ClienteId       INT NOT NULL CONSTRAINT FK_Vehiculos_Clientes REFERENCES dbo.Clientes(ClienteId),
    Placa           NVARCHAR(15)  NOT NULL CONSTRAINT UQ_Vehiculos_Placa UNIQUE,
    Marca           NVARCHAR(50)  NOT NULL,
    Modelo          NVARCHAR(50)  NOT NULL,
    Anio            INT NOT NULL CONSTRAINT CK_Vehiculos_Anio CHECK (Anio BETWEEN 1950 AND 2100),
    Color           NVARCHAR(30)  NULL,
    Vin             NVARCHAR(30)  NULL,
    Activo          BIT NOT NULL CONSTRAINT DF_Vehiculos_Activo DEFAULT (1),
    FechaRegistro   DATETIME2(0) NOT NULL CONSTRAINT DF_Vehiculos_FechaRegistro DEFAULT (SYSDATETIME())
);
GO

/* =====================================================================
   ÓRDENES DE TRABAJO
   Flujo: RECIBIDO -> EN_DIAGNOSTICO -> EN_REPARACION <-> ESPERANDO_REPUESTOS
          -> FINALIZADO -> ENTREGADO        (CANCELADO antes de reparar)
   ===================================================================== */

CREATE TABLE dbo.OrdenesTrabajo (
    OrdenId           INT IDENTITY(1,1) CONSTRAINT PK_OrdenesTrabajo PRIMARY KEY,
    NumeroOrden       AS ('OT-' + RIGHT('000000' + CAST(OrdenId AS VARCHAR(10)), 6)) PERSISTED,
    VehiculoId        INT NOT NULL CONSTRAINT FK_Ordenes_Vehiculos REFERENCES dbo.Vehiculos(VehiculoId),
    ClienteId         INT NOT NULL CONSTRAINT FK_Ordenes_Clientes REFERENCES dbo.Clientes(ClienteId),
    RecepcionistaId   INT NOT NULL CONSTRAINT FK_Ordenes_Recepcionista REFERENCES dbo.Usuarios(UsuarioId),
    MecanicoId        INT NULL     CONSTRAINT FK_Ordenes_Mecanico REFERENCES dbo.Usuarios(UsuarioId),
    FechaIngreso      DATETIME2(0) NOT NULL CONSTRAINT DF_Ordenes_FechaIngreso DEFAULT (SYSDATETIME()),
    FechaEstimada     DATE NULL,
    Kilometraje       INT NOT NULL CONSTRAINT CK_Ordenes_Kilometraje CHECK (Kilometraje >= 0),
    NivelCombustible  NVARCHAR(10) NOT NULL,
    MotivoIngreso     NVARCHAR(500) NOT NULL,
    Observaciones     NVARCHAR(500) NULL,
    Prioridad         NVARCHAR(10) NOT NULL CONSTRAINT DF_Ordenes_Prioridad DEFAULT ('NORMAL')
                      CONSTRAINT CK_Ordenes_Prioridad CHECK (Prioridad IN ('BAJA','NORMAL','ALTA','URGENTE')),
    Estado            NVARCHAR(25) NOT NULL CONSTRAINT DF_Ordenes_Estado DEFAULT ('RECIBIDO')
                      CONSTRAINT CK_Ordenes_Estado CHECK (Estado IN ('RECIBIDO','EN_DIAGNOSTICO','EN_REPARACION',
                                                                     'ESPERANDO_REPUESTOS','FINALIZADO','ENTREGADO','CANCELADO')),
    FechaSalida       DATETIME2(0) NULL,
    UsuarioSalidaId   INT NULL CONSTRAINT FK_Ordenes_UsuarioSalida REFERENCES dbo.Usuarios(UsuarioId),
    ObservacionSalida NVARCHAR(500) NULL
);
GO
CREATE INDEX IX_Ordenes_Estado ON dbo.OrdenesTrabajo(Estado);
CREATE INDEX IX_Ordenes_Mecanico ON dbo.OrdenesTrabajo(MecanicoId);
GO

CREATE TABLE dbo.HistorialOrden (
    HistorialId     INT IDENTITY(1,1) CONSTRAINT PK_HistorialOrden PRIMARY KEY,
    OrdenId         INT NOT NULL CONSTRAINT FK_Historial_Ordenes REFERENCES dbo.OrdenesTrabajo(OrdenId),
    EstadoAnterior  NVARCHAR(25) NULL,
    EstadoNuevo     NVARCHAR(25) NOT NULL,
    UsuarioId       INT NOT NULL CONSTRAINT FK_Historial_Usuarios REFERENCES dbo.Usuarios(UsuarioId),
    Comentario      NVARCHAR(300) NULL,
    Fecha           DATETIME2(0) NOT NULL CONSTRAINT DF_Historial_Fecha DEFAULT (SYSDATETIME())
);
GO

CREATE TABLE dbo.Diagnosticos (
    DiagnosticoId       INT IDENTITY(1,1) CONSTRAINT PK_Diagnosticos PRIMARY KEY,
    OrdenId             INT NOT NULL CONSTRAINT FK_Diagnosticos_Ordenes REFERENCES dbo.OrdenesTrabajo(OrdenId),
    MecanicoId          INT NOT NULL CONSTRAINT FK_Diagnosticos_Usuarios REFERENCES dbo.Usuarios(UsuarioId),
    FallaDetectada      NVARCHAR(500) NOT NULL,
    ReparacionRequerida NVARCHAR(500) NOT NULL,
    Severidad           NVARCHAR(10) NOT NULL
                        CONSTRAINT CK_Diagnosticos_Severidad CHECK (Severidad IN ('BAJA','MEDIA','ALTA','CRITICA')),
    CostoManoObra       DECIMAL(12,2) NOT NULL CONSTRAINT DF_Diagnosticos_Costo DEFAULT (0)
                        CONSTRAINT CK_Diagnosticos_Costo CHECK (CostoManoObra >= 0),
    Observaciones       NVARCHAR(500) NULL,
    Fecha               DATETIME2(0) NOT NULL CONSTRAINT DF_Diagnosticos_Fecha DEFAULT (SYSDATETIME())
);
GO

/* =====================================================================
   BODEGA / INVENTARIO
   ===================================================================== */

CREATE TABLE dbo.Categorias (
    CategoriaId     INT IDENTITY(1,1) CONSTRAINT PK_Categorias PRIMARY KEY,
    Nombre          NVARCHAR(60)  NOT NULL CONSTRAINT UQ_Categorias_Nombre UNIQUE,
    Descripcion     NVARCHAR(200) NULL,
    Activo          BIT NOT NULL CONSTRAINT DF_Categorias_Activo DEFAULT (1)
);
GO

CREATE TABLE dbo.Repuestos (
    RepuestoId      INT IDENTITY(1,1) CONSTRAINT PK_Repuestos PRIMARY KEY,
    Codigo          NVARCHAR(30)  NOT NULL CONSTRAINT UQ_Repuestos_Codigo UNIQUE,
    Nombre          NVARCHAR(120) NOT NULL,
    Descripcion     NVARCHAR(300) NULL,
    CategoriaId     INT NOT NULL CONSTRAINT FK_Repuestos_Categorias REFERENCES dbo.Categorias(CategoriaId),
    Marca           NVARCHAR(50)  NULL,
    PrecioCosto     DECIMAL(12,2) NOT NULL CONSTRAINT CK_Repuestos_PrecioCosto CHECK (PrecioCosto >= 0),
    PrecioVenta     DECIMAL(12,2) NOT NULL CONSTRAINT CK_Repuestos_PrecioVenta CHECK (PrecioVenta >= 0),
    Stock           INT NOT NULL CONSTRAINT DF_Repuestos_Stock DEFAULT (0)
                    CONSTRAINT CK_Repuestos_Stock CHECK (Stock >= 0),
    StockMinimo     INT NOT NULL CONSTRAINT DF_Repuestos_StockMinimo DEFAULT (0),
    Ubicacion       NVARCHAR(50)  NULL,
    Activo          BIT NOT NULL CONSTRAINT DF_Repuestos_Activo DEFAULT (1),
    FechaRegistro   DATETIME2(0) NOT NULL CONSTRAINT DF_Repuestos_FechaRegistro DEFAULT (SYSDATETIME())
);
GO

CREATE TABLE dbo.MovimientosInventario (
    MovimientoId    INT IDENTITY(1,1) CONSTRAINT PK_MovimientosInventario PRIMARY KEY,
    RepuestoId      INT NOT NULL CONSTRAINT FK_Movimientos_Repuestos REFERENCES dbo.Repuestos(RepuestoId),
    Tipo            NVARCHAR(10) NOT NULL CONSTRAINT CK_Movimientos_Tipo CHECK (Tipo IN ('ENTRADA','SALIDA','AJUSTE')),
    Cantidad        INT NOT NULL,
    StockAnterior   INT NOT NULL,
    StockNuevo      INT NOT NULL,
    Referencia      NVARCHAR(50)  NULL,
    Observacion     NVARCHAR(250) NULL,
    UsuarioId       INT NOT NULL CONSTRAINT FK_Movimientos_Usuarios REFERENCES dbo.Usuarios(UsuarioId),
    Fecha           DATETIME2(0) NOT NULL CONSTRAINT DF_Movimientos_Fecha DEFAULT (SYSDATETIME())
);
GO
CREATE INDEX IX_Movimientos_Repuesto ON dbo.MovimientosInventario(RepuestoId, Fecha DESC);
GO

CREATE TABLE dbo.SolicitudesRepuesto (
    SolicitudId     INT IDENTITY(1,1) CONSTRAINT PK_SolicitudesRepuesto PRIMARY KEY,
    OrdenId         INT NOT NULL CONSTRAINT FK_Solicitudes_Ordenes REFERENCES dbo.OrdenesTrabajo(OrdenId),
    RepuestoId      INT NOT NULL CONSTRAINT FK_Solicitudes_Repuestos REFERENCES dbo.Repuestos(RepuestoId),
    Cantidad        INT NOT NULL CONSTRAINT CK_Solicitudes_Cantidad CHECK (Cantidad > 0),
    SolicitanteId   INT NOT NULL CONSTRAINT FK_Solicitudes_Solicitante REFERENCES dbo.Usuarios(UsuarioId),
    FechaSolicitud  DATETIME2(0) NOT NULL CONSTRAINT DF_Solicitudes_Fecha DEFAULT (SYSDATETIME()),
    Estado          NVARCHAR(12) NOT NULL CONSTRAINT DF_Solicitudes_Estado DEFAULT ('PENDIENTE')
                    CONSTRAINT CK_Solicitudes_Estado CHECK (Estado IN ('PENDIENTE','DESPACHADO','RECHAZADO','CANCELADO')),
    BodegueroId     INT NULL CONSTRAINT FK_Solicitudes_Bodeguero REFERENCES dbo.Usuarios(UsuarioId),
    FechaRespuesta  DATETIME2(0) NULL,
    PrecioUnitario  DECIMAL(12,2) NULL,
    Observacion     NVARCHAR(250) NULL,
    MotivoRechazo   NVARCHAR(250) NULL
);
GO
CREATE INDEX IX_Solicitudes_Estado ON dbo.SolicitudesRepuesto(Estado);
GO

/* =====================================================================
   FACTURACIÓN 
   ===================================================================== */

CREATE TABLE dbo.Facturas (
    FacturaId           INT IDENTITY(1,1) CONSTRAINT PK_Facturas PRIMARY KEY,
    NumeroFactura       AS ('FAC-' + RIGHT('000000' + CAST(FacturaId AS VARCHAR(10)), 6)) PERSISTED,
    OrdenId             INT NOT NULL CONSTRAINT FK_Facturas_Ordenes REFERENCES dbo.OrdenesTrabajo(OrdenId),
    ClienteId           INT NOT NULL CONSTRAINT FK_Facturas_Clientes REFERENCES dbo.Clientes(ClienteId),
    NitCliente          NVARCHAR(20)  NOT NULL,
    NombreCliente       NVARCHAR(170) NOT NULL,
    DireccionCliente    NVARCHAR(250) NULL,
    FechaEmision        DATETIME2(0) NOT NULL CONSTRAINT DF_Facturas_Fecha DEFAULT (SYSDATETIME()),
    SubtotalManoObra    DECIMAL(12,2) NOT NULL,
    SubtotalRepuestos   DECIMAL(12,2) NOT NULL,
    Descuento           DECIMAL(12,2) NOT NULL CONSTRAINT DF_Facturas_Descuento DEFAULT (0)
                        CONSTRAINT CK_Facturas_Descuento CHECK (Descuento >= 0),
    Total               DECIMAL(12,2) NOT NULL,
    PorcentajeImpuesto  DECIMAL(5,2)  NOT NULL,
    BaseImponible       DECIMAL(12,2) NOT NULL,
    Impuesto            DECIMAL(12,2) NOT NULL,
    MetodoPago          NVARCHAR(15) NOT NULL
                        CONSTRAINT CK_Facturas_MetodoPago CHECK (MetodoPago IN ('EFECTIVO','TARJETA','TRANSFERENCIA')),
    Estado              NVARCHAR(10) NOT NULL CONSTRAINT DF_Facturas_Estado DEFAULT ('EMITIDA')
                        CONSTRAINT CK_Facturas_Estado CHECK (Estado IN ('EMITIDA','ANULADA')),
    UsuarioId           INT NOT NULL CONSTRAINT FK_Facturas_Usuarios REFERENCES dbo.Usuarios(UsuarioId),
    MotivoAnulacion     NVARCHAR(250) NULL,
    FechaAnulacion      DATETIME2(0) NULL
);
GO
-- Solo puede existir una factura vigente por orden
CREATE UNIQUE INDEX UX_Facturas_OrdenEmitida ON dbo.Facturas(OrdenId) WHERE Estado = 'EMITIDA';
GO

CREATE TABLE dbo.DetalleFactura (
    DetalleId       INT IDENTITY(1,1) CONSTRAINT PK_DetalleFactura PRIMARY KEY,
    FacturaId       INT NOT NULL CONSTRAINT FK_Detalle_Facturas REFERENCES dbo.Facturas(FacturaId) ON DELETE CASCADE,
    Tipo            NVARCHAR(10) NOT NULL CONSTRAINT CK_Detalle_Tipo CHECK (Tipo IN ('SERVICIO','REPUESTO')),
    Descripcion     NVARCHAR(300) NOT NULL,
    Cantidad        INT NOT NULL,
    PrecioUnitario  DECIMAL(12,2) NOT NULL,
    Subtotal        DECIMAL(12,2) NOT NULL,
    RepuestoId      INT NULL CONSTRAINT FK_Detalle_Repuestos REFERENCES dbo.Repuestos(RepuestoId)
);
GO

/* =====================================================================
   DATOS INICIALES
   ===================================================================== */

INSERT INTO dbo.Roles (Nombre, Descripcion, EsSistema) VALUES
 (N'Administrador', N'Gestión global del sistema, parametrización y asignación de permisos', 1),
 (N'Recepción',     N'Registro de ingreso/salida de vehículos y atención de clientes', 0),
 (N'Mecánico',      N'Diagnósticos, solicitud de repuestos y actualización de reparaciones', 0),
 (N'Bodeguero',     N'Gestión de inventario, despacho y control de existencias', 0);
GO

INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, Grupo, Icono, Orden) VALUES
 (N'CLIENTES',     N'Clientes',              N'Registro y consulta de clientes',                    N'Recepción',      N'bi-people',            10),
 (N'VEHICULOS',    N'Vehículos',             N'Registro y consulta de vehículos',                   N'Recepción',      N'bi-car-front',         20),
 (N'ORDENES',      N'Órdenes de trabajo',    N'Ingreso, asignación de mecánico y salida',           N'Recepción',      N'bi-clipboard-check',   30),
 (N'DIAGNOSTICOS', N'Diagnósticos',          N'Registro de fallas y reparaciones requeridas',       N'Taller',         N'bi-search',            40),
 (N'REPARACIONES', N'Reparaciones',          N'Tablero de trabajos y estado de reparación',         N'Taller',         N'bi-tools',             50),
 (N'SOLICITUDES',  N'Solicitud de repuestos',N'Solicitud de repuestos para una reparación',         N'Taller',         N'bi-cart-plus',         60),
 (N'INVENTARIO',   N'Inventario',            N'Repuestos, categorías y movimientos de stock',       N'Bodega',         N'bi-box-seam',          70),
 (N'DESPACHOS',    N'Despachos',             N'Atención de solicitudes de repuestos',               N'Bodega',         N'bi-truck',             80),
 (N'FACTURACION',  N'Facturación',           N'Emisión y anulación de facturas',                    N'Caja',           N'bi-receipt',           90),
 (N'USUARIOS',     N'Usuarios',              N'Administración de usuarios del sistema',             N'Administración', N'bi-person-gear',      100),
 (N'ROLES',        N'Roles y permisos',      N'Roles y permisos por módulo',                        N'Administración', N'bi-shield-lock',      110),
 (N'PARAMETROS',   N'Parámetros',            N'Configuración general del taller',                   N'Administración', N'bi-sliders',          120),
 (N'BITACORA',     N'Bitácora',              N'Historial de operaciones del sistema',               N'Administración', N'bi-journal-text',     130);
GO

-- Administrador: acceso total
INSERT INTO dbo.RolPermisos (RolId, ModuloId, PuedeVer, PuedeCrear, PuedeEditar, PuedeEliminar)
SELECT 1, ModuloId, 1, 1, 1, 1 FROM dbo.Modulos;

-- Recepción, Mecánico y Bodeguero: (Ver, Crear, Editar, Eliminar)
DECLARE @p TABLE (Rol INT, Codigo NVARCHAR(30), V BIT, C BIT, E BIT, D BIT);
INSERT INTO @p VALUES
 (2, N'CLIENTES',     1, 1, 1, 0),
 (2, N'VEHICULOS',    1, 1, 1, 0),
 (2, N'ORDENES',      1, 1, 1, 0),
 (2, N'DIAGNOSTICOS', 1, 0, 0, 0),
 (2, N'REPARACIONES', 1, 0, 0, 0),
 (2, N'FACTURACION',  1, 1, 0, 0),
 (3, N'CLIENTES',     1, 0, 0, 0),
 (3, N'VEHICULOS',    1, 0, 0, 0),
 (3, N'ORDENES',      1, 0, 0, 0),
 (3, N'DIAGNOSTICOS', 1, 1, 1, 0),
 (3, N'REPARACIONES', 1, 0, 1, 0),
 (3, N'SOLICITUDES',  1, 1, 0, 1),
 (3, N'INVENTARIO',   1, 0, 0, 0),
 (4, N'ORDENES',      1, 0, 0, 0),
 (4, N'INVENTARIO',   1, 1, 1, 1),
 (4, N'DESPACHOS',    1, 0, 1, 0);

INSERT INTO dbo.RolPermisos (RolId, ModuloId, PuedeVer, PuedeCrear, PuedeEditar, PuedeEliminar)
SELECT r.RolId, m.ModuloId, ISNULL(p.V, 0), ISNULL(p.C, 0), ISNULL(p.E, 0), ISNULL(p.D, 0)
FROM dbo.Roles r
CROSS JOIN dbo.Modulos m
LEFT JOIN @p p ON p.Rol = r.RolId AND p.Codigo = m.Codigo
WHERE r.RolId <> 1;
GO

-- Contraseñas con hash BCrypt:  admin = Admin123!   demás = Taller123!
INSERT INTO dbo.Usuarios (NombreCompleto, NombreUsuario, Correo, Telefono, PasswordHash, RolId) VALUES
 (N'Administrador General', N'admin',     N'admin@tallerdw.com',     N'5555-0001', N'$2a$11$A9SOhHjNkRPe6vciKeOHtOFdN9M81jKYzePP89JEBnBMgQ.ZOwQpS', 1),
 (N'Ana Lucía Morales',     N'recepcion', N'recepcion@tallerdw.com', N'5555-0002', N'$2a$11$JXWlIcA7IQZAbyr4UCIB7.eYOX4BQumbJMdl7hIqx1QOWOwLVmVDu', 2),
 (N'Carlos Méndez',         N'mecanico',  N'cmendez@tallerdw.com',   N'5555-0003', N'$2a$11$JXWlIcA7IQZAbyr4UCIB7.eYOX4BQumbJMdl7hIqx1QOWOwLVmVDu', 3),
 (N'Luis Ramírez',          N'mecanico2', N'lramirez@tallerdw.com',  N'5555-0004', N'$2a$11$JXWlIcA7IQZAbyr4UCIB7.eYOX4BQumbJMdl7hIqx1QOWOwLVmVDu', 3),
 (N'Jorge Pérez',           N'bodega',    N'bodega@tallerdw.com',    N'5555-0005', N'$2a$11$JXWlIcA7IQZAbyr4UCIB7.eYOX4BQumbJMdl7hIqx1QOWOwLVmVDu', 4);
GO

INSERT INTO dbo.Parametros (Clave, Valor, Descripcion, Tipo) VALUES
 (N'NOMBRE_TALLER',     N'Taller DW',                N'Nombre comercial del taller',            N'TEXTO'),
 (N'NIT_TALLER',        N'1234567-8',                N'NIT del taller para facturación',        N'TEXTO'),
 (N'DIRECCION_TALLER',  N'Ciudad de Guatemala',      N'Dirección del taller',                   N'TEXTO'),
 (N'TELEFONO_TALLER',   N'2222-0000',                N'Teléfono de contacto',                   N'TEXTO'),
 (N'CORREO_TALLER',     N'contacto@tallerdw.com',    N'Correo de contacto',                     N'TEXTO'),
 (N'MONEDA',            N'Q',                        N'Símbolo de moneda',                      N'TEXTO'),
 (N'IVA_PORCENTAJE',    N'12',                       N'Porcentaje de IVA (incluido en precios)', N'NUMERO'),
 (N'DESCUENTO_MAXIMO',  N'15',                       N'Descuento máximo permitido en factura (%)', N'NUMERO');
GO

INSERT INTO dbo.Categorias (Nombre, Descripcion) VALUES
 (N'Motor',        N'Componentes internos y externos del motor'),
 (N'Frenos',       N'Pastillas, discos, tambores y líquidos'),
 (N'Suspensión',   N'Amortiguadores, rótulas y bujes'),
 (N'Eléctrico',    N'Baterías, bujías, alternadores y sensores'),
 (N'Lubricantes',  N'Aceites y grasas'),
 (N'Filtros',      N'Filtros de aceite, aire, combustible y cabina'),
 (N'Transmisión',  N'Clutch, cajas y juntas'),
 (N'Refrigeración',N'Radiadores, termostatos y refrigerantes');
GO

INSERT INTO dbo.Repuestos (Codigo, Nombre, Descripcion, CategoriaId, Marca, PrecioCosto, PrecioVenta, Stock, StockMinimo, Ubicacion) VALUES
 (N'FIL-ACE-001', N'Filtro de aceite',            N'Filtro de aceite universal rosca 3/4',  6, N'Bosch',     35.00,   60.00, 40, 10, N'A-01'),
 (N'FIL-AIR-002', N'Filtro de aire',              N'Filtro de aire para motor 1.6-2.0',     6, N'Mann',      55.00,   95.00, 25,  8, N'A-02'),
 (N'LUB-5W30-01', N'Aceite sintético 5W-30 (1/4)',N'Aceite de motor sintético, cuarto',     5, N'Mobil',     42.00,   70.00, 80, 20, N'B-01'),
 (N'LUB-15W40-2', N'Aceite mineral 15W-40 (1/4)', N'Aceite de motor mineral, cuarto',       5, N'Castrol',   30.00,   50.00, 60, 20, N'B-02'),
 (N'FRE-PAS-001', N'Pastillas de freno delanteras', N'Juego de pastillas cerámicas',        2, N'Brembo',   180.00,  320.00, 12,  4, N'C-01'),
 (N'FRE-DIS-002', N'Disco de freno ventilado',    N'Disco delantero ventilado',             2, N'Brembo',   260.00,  450.00,  6,  2, N'C-02'),
 (N'FRE-LIQ-003', N'Líquido de frenos DOT 4',     N'Botella 500 ml',                        2, N'Bosch',     38.00,   65.00, 18,  5, N'C-03'),
 (N'SUS-AMO-001', N'Amortiguador delantero',      N'Amortiguador a gas',                    3, N'Monroe',   420.00,  690.00,  4,  2, N'D-01'),
 (N'ELE-BUJ-001', N'Bujía de iridio',             N'Bujía de larga duración',               4, N'NGK',       45.00,   85.00, 32, 12, N'E-01'),
 (N'ELE-BAT-002', N'Batería 12V 45Ah',            N'Batería libre de mantenimiento',        4, N'LTH',      650.00,  950.00,  3,  2, N'E-02'),
 (N'TRA-KIT-001', N'Kit de clutch',               N'Disco, plato y collarín',               7, N'Valeo',   1100.00, 1750.00,  2,  1, N'F-01'),
 (N'REF-REF-001', N'Refrigerante 50/50 (galón)',  N'Anticongelante listo para usar',        8, N'Prestone',  60.00,  110.00, 15,  5, N'G-01');
GO

INSERT INTO dbo.MovimientosInventario (RepuestoId, Tipo, Cantidad, StockAnterior, StockNuevo, Referencia, Observacion, UsuarioId)
SELECT RepuestoId, 'ENTRADA', Stock, 0, Stock, N'INV-INICIAL', N'Carga inicial de inventario', 5
FROM dbo.Repuestos;
GO

INSERT INTO dbo.Clientes (Nombres, Apellidos, Nit, Dpi, Telefono, Correo, Direccion) VALUES
 (N'María José', N'García López',   N'4589632-1', N'2589641230101', N'5021-4587', N'mjgarcia@correo.com', N'Zona 10, Ciudad de Guatemala'),
 (N'Pedro',      N'Hernández Ruiz', N'CF',        N'3014785210101', N'4478-1236', NULL,                   N'Mixco, Guatemala'),
 (N'Sofía',      N'Castillo Pérez', N'7852146-3', NULL,             N'3365-9874', N'scastillo@correo.com', N'Villa Nueva, Guatemala');
GO

INSERT INTO dbo.Vehiculos (ClienteId, Placa, Marca, Modelo, Anio, Color, Vin) VALUES
 (1, N'P-512DFG', N'Toyota',     N'Corolla', 2019, N'Blanco', N'JTDBR32E720045871'),
 (1, N'P-874KLM', N'Honda',      N'CR-V',    2021, N'Gris',   NULL),
 (2, N'P-236HJK', N'Nissan',     N'Sentra',  2016, N'Negro',  NULL),
 (3, N'P-963QWE', N'Mazda',      N'CX-5',    2022, N'Rojo',   NULL);
GO

SET NOEXEC OFF;
GO
PRINT 'Base de datos TallerDW creada correctamente.';
GO
