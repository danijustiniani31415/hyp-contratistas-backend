-- =============================================================================
-- 002_migracion_datos.sql
-- Sprint 7 — Migración de datos PowerApps → ss_empresa_contratista
-- + correlación con companies legacy + carga de registros modelo
--
-- Notas operativas:
--   * Las contraseñas NO se hashean en SQL puro (BCrypt sólo en backend).
--     Cada empresa migrada queda con password_hash = 'PENDIENTE_RESET'.
--     El admin debe disparar PATCH /api/v1/habilitacion/empresas/{id}/password
--     para asignar una contraseña real antes de habilitar el login.
--   * El script es idempotente: usa ON CONFLICT (ruc) DO NOTHING en las
--     empresas con RUC, y ON CONFLICT DO NOTHING en los registros modelo.
--     Empresas sin RUC se insertan siempre — verificar duplicados antes de
--     re-ejecutar si fuera el caso.
-- =============================================================================


-- =============================================================================
-- SECCIÓN 1 — INSERT DE EMPRESAS CONTRATISTAS DESDE CSV PowerApps
-- =============================================================================
-- Filtrar SOLO las que cumplen:
--   * ActivoRetirado <> 'Retirado'
--   * Ruc no vacío (si está vacío usar NULL)
-- Las primeras 50 filas del CSV ordenado por fecha de creación van abajo.
-- El resto de filas se completan manualmente o vía POST /api/v1/habilitacion/empresas
-- desde el formulario.
--
-- NOTA: el dataset completo está en ListaContratistas.csv. Las filas que siguen
-- son una muestra representativa con la estructura final lista para reemplazar
-- los placeholders por los valores del CSV real (RUC, razón social, emails, etc.).
-- =============================================================================

INSERT INTO ss_empresa_contratista (
    ruc, razon_social, nombre_comercial, rubro, direccion,
    password_hash, email_gerente, email_admin, email_residente,
    email_ssoma, tipo, activo, activo_retirado, id_legacy
) VALUES
    (NULLIF('20604826854',''), 'Seshat Inmobiliaria S.A.C.', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('sjustiniani@abril.pe',''), NULLIF('',''),
     NULLIF('sjustiniani@abril.pe',''), 'ABRIL', true,
     COALESCE(NULLIF('',''),'Activo'), 67),

    (NULLIF('20605891714',''), 'Catania Inmobiliaria S.A.C.', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('eillatopa@abril.pe',''), NULLIF('',''),
     NULLIF('jbernabel@abril.pe',''), 'ABRIL', true,
     COALESCE(NULLIF('',''),'Activo'), 68),

    (NULLIF('20605487450',''), 'Thabit Inmobiliaria S.A.C.', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('ccarrillo@abril.pe',''), NULLIF('',''),
     NULLIF('lpalencia@abril.pe',''), 'ABRIL', true,
     COALESCE(NULLIF('',''),'Activo'), 69),

    (NULLIF('20551959946',''), 'LUMBRERAS CONSTRUCCIONES & PROYECTOS SAC', NULLIF('',''),
     NULLIF('Instalaciones eléctricas',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('lumbreras.sac@gmail.com',''),
     NULLIF('administracion@lumbreras.pe',''), NULLIF('',''),
     NULLIF('area.ssoma@lumbreras.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 112),

    (NULLIF('20524173256',''), 'Forward Imports S.A.', NULLIF('',''),
     NULLIF('Instalación Contra Incendios',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('jrodriguez@fisaperu.com',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('fnovoa@fisaperu.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 125),

    (NULLIF('20608912739',''), 'RED VIAS S.A.C.', NULLIF('',''),
     NULLIF('Pintura',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('wchamorro@redvias.com',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('clcornelio1@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 155),

    (NULLIF('20602517625',''), 'NEQFAL Servicios Generales E.I.R.L', NULLIF('',''),
     NULLIF('Pintura',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('cyaneztornero@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 162),

    (NULLIF('20563278600',''), '2 A INGENIEROS S.A.C.', NULLIF('',''),
     NULLIF('Instalación de acero',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('vancajima@2aingenieros.com',''),
     NULLIF('mchuque@2aingenieros.com',''), NULLIF('',''),
     NULLIF('seguridad.2aingenieros@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 164),

    (NULLIF('20600629418',''), 'MAYO MATERIALES E INSTALACIONES E.I.R.L.', NULLIF('',''),
     NULLIF('Piso laminado',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('CRODRIGUEZ@MAYO.PE',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('IMPORTACIONES@MAYO.PE',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 165),

    (NULLIF('20108026104',''), 'UEZU INGENIEROS SRL', NULLIF('',''),
     NULLIF('Instalaciones mecánicas',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('uezumaster@uezuperu.com',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('seguridad2@uezuperu.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 167),

    (NULLIF('20551790003',''), 'Silva & Junior SAC', NULLIF('',''),
     NULLIF('Mamparas y ventanas',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('maurorsr@silvajuniorgroup.com',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('alexandrapeceros913@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 185),

    (NULLIF('20607966240',''), 'Fenix Proyectos y Edificaciones S.A.C.', NULLIF('',''),
     NULLIF('Enchape de pisos',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('fenix.pye@hotmail.com',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('fenix.pye@hotmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 199),

    (NULLIF('20377735146',''), 'UNI-SPAN PERU S.A.', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('ruben.horna@unispan.com.pe',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('carlos.perez@unispan.com.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 200),

    (NULLIF('20605182802',''), 'WOLF PROCORAC EIRL', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('wolfproperu@gmail.com',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ssomapdr@wolfpro.com.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 210),

    (NULLIF('20603425414',''), 'ACEROS FV PERU SAC', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('andrea.be.2404@hotmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 211),

    (NULLIF('20605639390',''), 'DECOR LOAYZA S.A.C', NULLIF('',''),
     NULLIF('Pintura',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('decorloayza@hotmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 212),

    (NULLIF('20430425138',''), 'MELANOVA SAC', NULLIF('',''),
     NULLIF('Muebles de cocina',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('gerencia@melanova.com',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('wendysalazar@melanova.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 233),

    (NULLIF('20550456746',''), 'CASEVIP S.R.L', NULLIF('',''),
     NULLIF('Vigilancia',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('GERENCIA@CASEVIP.COM.PE',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ANALISTA_SIG_SSOMA@CASEVIP.COM.PE',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 234),

    (NULLIF('20605217789',''), 'JJMV FAEMSA SAC', NULLIF('',''),
     NULLIF('Soportes metálicos',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('l.canales@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 241),

    (NULLIF('20303474634',''), 'Lima Grass S.A', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ventas3@corporaciongrubal.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 245),

    (NULLIF('20524764530',''), 'SKY PAINT AND CLEAN EIRL', NULLIF('',''),
     NULLIF('Andamios y elevadores',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('SSOMA@SKYPASE.COM',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 247),

    (NULLIF('10125604',''), 'HENRY MARTIN CARITA COTERA', NULLIF('',''),
     NULLIF('Drywall',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('JFERNANDEZ@ABRIL.PE',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 248),

    (NULLIF('20563018799',''), 'H&R EURO STONE SAC', NULLIF('',''),
     NULLIF('Granito y marmol',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('grodriguez@marmoleshyr.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 255),

    (NULLIF('20602696643',''), 'ANDAMIOS ELECTRICOS INNOVA S.A.C', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('info@grupoinnova.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 257),

    (NULLIF('20610677798',''), 'AMG STONE - PERU SAC', NULLIF('',''),
     NULLIF('Tableros de granito',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('asesordeproyectos3@decorgrama.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 260),

    (NULLIF('20606032871',''), 'EYM INGENIEROS CONTRATISTAS SAC', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('jorgetorreseym@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 266),

    (NULLIF('20607890898',''), 'CORPORACIÓN INMOBILIARIA GCG SAC', NULLIF('',''),
     NULLIF('Pintura y Tarrajeo',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('rrhh2.gcg.peru@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 270),

    (NULLIF('20556964620',''), 'betondecken sac', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('proyectos.clientes@betondecken.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 272),

    (NULLIF('20600807260',''), 'ANCELSA S.A.C.', NULLIF('',''),
     NULLIF('Andamios colgantes',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('tjimenez@ancelsa.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 274),

    (NULLIF('20543189872',''), 'JS MUEBLES Y DISEÑO SAC', NULLIF('',''),
     NULLIF('Muebles de baño y closets',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('jsmueblesydiseno@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 277),

    (NULLIF('20603204451',''), 'HAMVA PROYECTOS S.A.C.', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('hamvaproyectos@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 285),

    (NULLIF('20509429538',''), 'GLOBAL IMPORT PERU SAC', NULLIF('',''),
     NULLIF('Alarmas contraincendios',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('proyectos@globalimportperu.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 298),

    (NULLIF('20523104404',''), 'ARES PERU SAC', NULLIF('',''),
     NULLIF('Puertas',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ingsst@aresperu.com.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 301),

    (NULLIF('20606667265',''), 'KLS Arquitectura y acero', NULLIF('',''),
     NULLIF('Pintura barandas',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ksantillan@klss.com.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 304),

    (NULLIF('20502318528',''), 'PROYECTOS D SAC', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('SSOMA@PROYECTOSDSAC.COM',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 313),

    (NULLIF('20607888273',''), 'RIOPROM SERVICIOC GENRALES S.A.C.', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ssoma.rioprom@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 315),

    (NULLIF('10098464285',''), 'GALINDO CONDE EMILIANO CESAR', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 320),

    (NULLIF('20515938983',''), 'Cadena de Suministros SAC', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('contacto@cdsservicios.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 337),

    (NULLIF('20100057523',''), 'ASCENSORES S.A.', NULLIF('',''),
     NULLIF('Ascensores',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('adrianatorres@ascensores-sa.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 341),

    (NULLIF('20562735811',''), 'VC MULTISERVICE EIRL', NULLIF('',''),
     NULLIF('Transporte de carga',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('jorge.villegas@vcmultiservice.com.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 343),

    (NULLIF('20601832390',''), 'ALFA CO SAC', NULLIF('',''),
     NULLIF('Instalaciones de gas',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ignacio.gomez@alfaco.com.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 354),

    (NULLIF('20611603640',''), 'DOMA INGENIERIA Y CONSTRUCCION SAC', NULLIF('',''),
     NULLIF('Sistema ACI',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('SUPERVISIONSST@DOMAICSAC.COM',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 355),

    (NULLIF('20605239421',''), 'MOBIUS COMPANY S.A.C.', NULLIF('',''),
     NULLIF('Instalación de melamine',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ssoma@mobius.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 357),

    (NULLIF('20297543653',''), 'UNION DE CONCRETERAS S.A.', NULLIF('',''),
     NULLIF('Concreto premezclado',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('cvelasquezm@unicon.com.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 358),

    (NULLIF('20604642508',''), 'CONTRATISTAS GENERALES BERROCAL SAC', NULLIF('',''),
     NULLIF('Carpintería metálica',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('CGB.SOMMA@GMAIL.COM',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 359),

    (NULLIF('20607456250',''), 'ITEC PERU SERVICIOS GENERALES SAC', NULLIF('',''),
     NULLIF('Puertas',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('carlos.santos.villegas@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 361),

    (NULLIF('20606130369',''), 'BENAUTE STONE SAC', NULLIF('',''),
     NULLIF('Granito',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('benautestone@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 364),

    (NULLIF('20610543007',''), 'ORGANICA ARQUITECTURA Y CONSTRUCCION E.I.R.L.', NULLIF('',''),
     NULLIF('Drywall',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('christianticona@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 366),

    (NULLIF('20512303961',''), 'INVERSIONES SERIVIAL SAC', NULLIF('',''),
     NULLIF('Espejos y cortavientos',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 367),

    (NULLIF('20604890391',''), 'RP MURAL SAC', NULLIF('',''),
     NULLIF('Papel mural',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('supervision1.rpmuralsac@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 371),

    (NULLIF('20602940170',''), 'GRUPO JMA CONTRATISTAS GENERALES SAC', NULLIF('',''),
     NULLIF('Revestimientos de melamine',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('MLOARTE@GRUPOJMACG.COM',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 375),

    (NULLIF('20601858259',''), 'WARAYANA PROYECTOS E INVERSIONES S.A.C.', NULLIF('',''),
     NULLIF('Albanilería',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ssoma@warayanaproyectos.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 382),

    (NULLIF('20510797524',''), 'ARPE CG SAC', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('ssoma@arpecontratistas.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 496),

    (NULLIF('20551503462',''), 'FLESAN ANCLAJES S.A.C.', NULLIF('',''),
     NULLIF('Tensado de anclajes',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('GCPOZO@FLESAN.COM.PE',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 524),

    (NULLIF('20605624210',''), 'CONSORCIO GARCIA & R A L S.A.C', NULLIF('',''),
     NULLIF('Demolición y excavación',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('kataczar@unac.edu.pe',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 578),

    (NULLIF('20603173156',''), 'BATALLA DE JUNIN S.A.C.', NULLIF('',''),
     NULLIF('Obras civiles',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('jefessoma@batalladejunin.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 630),

    (NULLIF('20513692111',''), 'VIFAMAP SAC', NULLIF('',''),
     NULLIF('Carpintería metálica',''), NULLIF('',''),
     'PENDIENTE_RESET', NULLIF('',''),
     NULLIF('',''), NULLIF('',''),
     NULLIF('maycolssoma.vifamapsac@gmail.com',''), 'CONTRATISTA', true,
     COALESCE(NULLIF('',''),'Activo'), 718)
ON CONFLICT DO NOTHING;


-- =============================================================================
-- SECCIÓN 2 — CORRELACIÓN ss_empresa_contratista.id_legacy ↔ companies.id
-- =============================================================================
-- Problema documentado en CONTEXT.md §11:
--   WorkerVinculacion.EmpresaId apunta a la tabla legacy `companies`,
--   NO a `ss_empresa_contratista.id`. Sin esta correlación, los chequeos de
--   ownership (contratista solo ve sus workers) y la columna empresa en
--   /trabajadores fallan en silencio (devuelven NULL o 403 espurios).
--
-- El siguiente SELECT genera el mapeo candidato por RUC. NO ejecuta UPDATE:
--   el admin debe revisar caso por caso (RUCs con typos, empresas que cambiaron
--   de razón social, duplicados en companies, etc.) y luego correr el UPDATE
--   manual al final.
-- =============================================================================

-- Paso 2.1 — preview de matches por RUC exacto
SELECT
    sec.id          AS nuevo_id,
    sec.ruc,
    sec.razon_social,
    c.id            AS legacy_id,
    c.ruc           AS legacy_ruc,
    c.razon_social  AS legacy_razon_social
FROM ss_empresa_contratista sec
LEFT JOIN companies c ON c.ruc = sec.ruc
WHERE c.id IS NOT NULL
ORDER BY sec.id;

-- Paso 2.2 — preview de matches por nombre (cuando no hay match por RUC)
SELECT
    sec.id          AS nuevo_id,
    sec.razon_social,
    c.id            AS legacy_id,
    c.razon_social  AS legacy_razon_social,
    similarity(LOWER(sec.razon_social), LOWER(c.razon_social)) AS score
FROM ss_empresa_contratista sec
LEFT JOIN companies c ON LOWER(c.razon_social) % LOWER(sec.razon_social)
WHERE sec.id_legacy IS NULL
  AND c.id IS NOT NULL
ORDER BY score DESC, sec.id;
-- (requiere extensión pg_trgm; si no está disponible, comentar este bloque)

-- Paso 2.3 — UPDATE manual una vez validado el mapeo
-- (descomentar y ejecutar tras revisar las filas anteriores)
--
-- UPDATE ss_empresa_contratista sec
-- SET id_legacy = c.id
-- FROM companies c
-- WHERE c.ruc = sec.ruc
--   AND sec.id_legacy IS DISTINCT FROM c.id;
--
-- Verificación post-UPDATE:
-- SELECT COUNT(*) AS sin_correlacion
-- FROM ss_empresa_contratista
-- WHERE id_legacy IS NULL;


-- =============================================================================
-- SECCIÓN 3 — REGISTROS MODELO (documentos públicos del SharePoint)
-- =============================================================================
-- Catálogo de documentos modelo expuesto vía
--   GET /api/v1/habilitacion/registros-modelo
-- (endpoint AllowAnonymous, sin login).
-- =============================================================================

INSERT INTO ss_registro_modelo (nombre, archivo_url, activo, orden) VALUES
    ('Difusion Procedimientos',                   'ModeloDocumentos/Difusion Procedimientos.xlsx',                   true,  1),
    ('Matriz Comunicacion',                       'ModeloDocumentos/Matriz Comunicacion.xlsx',                       true,  2),
    ('Modelo de Entrega de Recomendaciones',      'ModeloDocumentos/Modelo de Entrega de Recomendaciones.docx',      true,  3),
    ('Modelo de Entrega de RISST',                'ModeloDocumentos/Modelo de Entrega de RISST.docx',                true,  4),
    ('Modelo de Registro de Entrega de EPPs',     'ModeloDocumentos/Modelo de Registro de Entrega de EPPs.xlsx',     true,  5),
    ('Modelo Induccion Hombre Nuevo',             'ModeloDocumentos/Modelo Induccion Hombre Nuevo.xlsx',             true,  6),
    ('Modelo IPERC',                              'ModeloDocumentos/Modelo IPERC.xlsx',                              true,  7),
    ('Modelo Matriz Comunicacion',                'ModeloDocumentos/Modelo Matriz Comunicacion.xlsx',                true,  8),
    ('Modelo Matriz de Aspectos e Impacto',       'ModeloDocumentos/Modelo Matriz de Aspectos e Impacto.xlsx',       true,  9),
    ('Modelo Organigrama',                        'ModeloDocumentos/Modelo Organigrama.xlsx',                        true, 10),
    ('Modelo PETS SSO Y MA',                      'ModeloDocumentos/Modelo PETS SSO Y MA.xlsx',                      true, 11),
    ('Modelo Plan Anual de SSOMA',                'ModeloDocumentos/Modelo Plan Anual de SSOMA.xlsx',                true, 12),
    ('SSO-FO-018.a ATS Albañileria',              'ModeloDocumentos/SSO-FO-018.a ATS Albanileria.xlsx',              true, 13),
    ('SSO-FO-018.b ATS Carpinteria',              'ModeloDocumentos/SSO-FO-018.b ATS Carpinteria.xlsx',              true, 14),
    ('SSO-FO-018.c ATS Enchape',                  'ModeloDocumentos/SSO-FO-018.c ATS Enchape.xlsx',                  true, 15),
    ('SSO-FO-018.d ATS Limpieza',                 'ModeloDocumentos/SSO-FO-018.d ATS Limpieza.xlsx',                 true, 16)
ON CONFLICT DO NOTHING;
