# GitHub Actions – Power Platform CI/CD

Manual de configuración para los workflows de exportación y despliegue de soluciones de Power Platform mediante GitHub Actions.

---

## Índice

1. [Requisitos previos](#1-requisitos-previos)
2. [Configuración en Azure – App Registration](#2-configuración-en-azure--app-registration)
3. [Registro del SPN en Power Platform](#3-registro-del-spn-en-power-platform)
4. [Configuración en GitHub](#4-configuración-en-github)
5. [Workflows disponibles](#5-workflows-disponibles)
6. [Estructura del repositorio](#6-estructura-del-repositorio)

---

## 1. Requisitos previos

- Acceso de **Global Administrator** o **Power Platform Administrator** en el tenant de Azure
- Licencia **Power Apps Premium** (o equivalente) en el tenant
- Repositorio GitHub con Actions habilitado

---

## 2. Configuración en Azure – App Registration

### 2.1 Crear la App Registration

1. Accede a [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID → Registros de aplicaciones**
2. Haz clic en **+ Nuevo registro**
3. Asígnale un nombre (ej. `PipelinesAzureDevops`) y registra
4. Anota el **Application (client) ID** y el **Directory (tenant) ID**

### 2.2 Crear un Client Secret

1. Dentro de la app → **Certificados y secretos → + Nuevo secreto de cliente**
2. Establece una fecha de expiración
3. **Copia el valor del secreto** inmediatamente (solo se muestra una vez)

### 2.3 Asignar el rol de Power Platform Administrator

1. En **Entra ID → Roles y administradores**
2. Busca **Administrador de Power Platform**
3. Haz clic en **+ Agregar asignaciones**
4. Busca la app registration y asígnala con tipo **Activo** y duración **Permanente**

---

## 3. Registro del SPN en Power Platform

Este paso es imprescindible para que el Service Principal pueda llamar a las APIs de administración de Power Platform. Debe ejecutarse **una única vez** por un administrador humano.

Abre PowerShell como administrador y ejecuta:

```powershell
# Instalar el módulo si no está disponible
Install-Module -Name Microsoft.PowerApps.Administration.PowerShell -Force

# Importar el módulo
Import-Module Microsoft.PowerApps.Administration.PowerShell

# Autenticarse con una cuenta de administrador humana (no el SPN)
Add-PowerAppsAccount -Endpoint prod

# Registrar el SPN como Admin Management Application
New-PowerAppManagementApp -ApplicationId "YOUR_APP_CLIENT_ID"
```

### Verificar el registro

```powershell
Get-PowerAppManagementApp -ApplicationId "YOUR_APP_CLIENT_ID"
```

Si devuelve el `ApplicationId`, el registro es correcto.

---

## 4. Configuración en GitHub

### 4.1 Crear el Environment

1. En el repositorio → **Settings → Environments → New environment**
2. Nómbralo exactamente: `Power Platform Dev Environment`
3. Opcionalmente, añade **required reviewers** para entornos de UAT o Producción

### 4.2 Configurar Secrets

Dentro del environment `Power Platform Dev Environment`, añade los siguientes secrets:

| Secret | Descripción |
|---|---|
| `PP_APP_ID` | Application (client) ID de la App Registration |
| `PP_CLIENT_SECRET` | Valor del client secret |
| `PP_DEV_ENV_URL` | URL del entorno de desarrollo (ej. `https://miorg.crm4.dynamics.com`) |

Para los workflows de UAT y Producción, crea los environments correspondientes (`Power Platform UAT Environment`, `Power Platform Production Environment`) con sus propios secrets:

| Secret | Descripción |
|---|---|
| `PP_UAT_ENV_URL` | URL del entorno de UAT |
| `PP_PROD_ENV_URL` | URL del entorno de Producción |

### 4.3 Configurar Variables

A nivel de repositorio (**Settings → Secrets and variables → Actions → Variables**):

| Variable | Descripción |
|---|---|
| `PP_TENANT_ID` | Directory (tenant) ID del tenant de Azure |

### 4.4 Habilitar permisos de escritura para Actions

En **Settings → Actions → General → Workflow permissions** selecciona **Read and write permissions**.

---

## 5. Workflows disponibles

### 5.1 Export Solution from Dev (`export-solutions.yml`)

Exporta la solución desde el entorno de desarrollo, la desempaqueta como código fuente y hace commit al repositorio.

**Activación:** Manual (`workflow_dispatch`)

**Qué hace:**
1. Exporta la solución en modo Unmanaged desde el entorno Dev
2. Desempaqueta el contenido en `solutions/`
3. Ajusta el `Solution.xml` para permitir empaquetado como Managed en el despliegue
4. Hace commit y push de los cambios al repositorio

---

### 5.2 Deploy Solution to Environment (`create-sandbox-and-deploy.yml`)

Despliega la solución en cualquier entorno existente a partir de su URL.

**Activación:** Manual (`workflow_dispatch`)

**Parámetros:**

| Parámetro | Descripción |
|---|---|
| `environment-url` | URL completa del entorno destino (ej. `https://miorg.crm4.dynamics.com`) |

**Qué hace:**
1. Empaqueta la solución como Managed desde el código fuente
2. Importa la solución en el entorno indicado (sin posibilidad de personalización)

---

### 5.3 Deploy Solution (`deploy-solution.yml`)

Despliega la solución eligiendo entre tres destinos predefinidos.

**Activación:** Manual (`workflow_dispatch`)

**Parámetros:**

| Parámetro | Descripción |
|---|---|
| `target` | Destino: `new-sandbox`, `uat` o `production` |
| `sandbox-name` | Nombre del sandbox (solo si `target = new-sandbox`) |

**Entornos y secrets utilizados:**

| Target | Environment de GitHub | Secret de URL |
|---|---|---|
| `new-sandbox` | `Power Platform Dev Environment` | Se construye a partir del nombre |
| `uat` | `Power Platform UAT Environment` | `PP_UAT_ENV_URL` |
| `production` | `Power Platform Production Environment` | `PP_PROD_ENV_URL` |

> **Recomendación:** Configura **required reviewers** en el environment de Producción para requerir aprobación manual antes del despliegue.

---

## 6. Estructura del repositorio

```
.github/
  workflows/
    export-solutions.yml          # Exportar solución desde Dev
    create-sandbox-and-deploy.yml # Desplegar en entorno por URL
    deploy-solution.yml           # Desplegar en sandbox / UAT / Producción
solutions/
  GithubActionsSolucinpersonalizada/
    Other/
      Solution.xml                # Metadata de la solución
    ...                           # Resto de componentes desempaquetados
```
