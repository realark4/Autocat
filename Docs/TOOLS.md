# Autocat CAD Tools Reference

This document provides a reference for all 30+ whitelisted CAD tools available to the AI Agent.

---

## 1. Drawing Information Tools

### `get_drawing_info`
- **Description**: Retrieves active document name, file path, current space, active layer, drawing units, and entity count.
- **Parameters**: None.

### `get_active_layer`
- **Description**: Returns the active layer name.
- **Parameters**: None.

### `get_layers`
- **Description**: Returns the list of all layers in the drawing with color, lock, and freeze status.
- **Parameters**: None.

### `create_layer`
- **Description**: Creates a new layer in AutoCAD.
- **Parameters**:
  - `layerName` (string, required): Name of the layer.
  - `color` (string, optional): ACI color name ("Red", "Green", "Cyan") or index (1-7).

### `set_current_layer`
- **Description**: Makes an existing layer the active layer.
- **Parameters**:
  - `layerName` (string, required): Name of the layer.

### `get_linear_units`
- **Description**: Returns the active linear units (Millimeters, Inches, Centimeters, Meters, etc.).
- **Parameters**: None.

---

## 2. Selection & Inspection Tools

### `get_selected_entities`
- **Description**: Returns geometric information, bounding boxes, handles, and layers for all currently selected objects.
- **Parameters**: None.

### `select_entities`
- **Description**: Sets active selection in AutoCAD by handle list.
- **Parameters**:
  - `handles` (array of strings, required): List of entity handles.

### `get_entity_info`
- **Description**: Returns detailed properties (type, coordinates, length/radius/area) of an entity.
- **Parameters**:
  - `handle` (string, required): Entity handle.

### `get_bounding_box`
- **Description**: Returns the 3D bounding box and center coordinate of an entity.
- **Parameters**:
  - `handle` (string, required): Entity handle.

### `get_entities_in_window`
- **Description**: Queries entities within a rectangular coordinate window.
- **Parameters**:
  - `pt1X`, `pt1Y`, `pt2X`, `pt2Y` (numbers, required).

### `get_entities_by_layer`
- **Description**: Returns all entities on a given layer.
- **Parameters**:
  - `layerName` (string, required).

### `get_entities_by_type`
- **Description**: Returns all entities of a specific CAD type (e.g. Line, Circle, Polyline).
- **Parameters**:
  - `entityType` (string, required).

---

## 3. Geometry Creation Tools

### `create_line`
- **Description**: Creates a straight line segment.
- **Parameters**:
  - `startX`, `startY`, `startZ` (numbers)
  - `endX`, `endY`, `endZ` (numbers)
  - `layer` (string, optional)

### `create_circle`
- **Description**: Creates a circle given center and radius (or diameter).
- **Parameters**:
  - `centerX`, `centerY`, `centerZ` (numbers)
  - `radius` (number, optional)
  - `diameter` (number, optional)
  - `layer` (string, optional)

### `create_arc`
- **Description**: Creates a circular arc given center, radius, start angle, and end angle (in degrees).
- **Parameters**:
  - `centerX`, `centerY`, `centerZ` (numbers)
  - `radius`, `startAngle`, `endAngle` (numbers)
  - `layer` (string, optional)

### `create_polyline`
- **Description**: Creates a 2D polyline through a list of vertex coordinates.
- **Parameters**:
  - `vertices` (array of [x, y], required)
  - `closed` (boolean, optional)
  - `layer` (string, optional)

### `create_rectangle`
- **Description**: Creates a rectangular polyline with optional corner radius fillets.
- **Parameters**:
  - `corner1X`, `corner1Y` (numbers, optional)
  - `corner2X`, `corner2Y` (numbers, optional)
  - `width`, `height` (numbers, optional)
  - `cornerRadius` (number, optional)
  - `layer` (string, optional)

### `create_ellipse`
- **Description**: Creates an ellipse given center, major axis vector, and radius ratio.
- **Parameters**:
  - `centerX`, `centerY`, `centerZ` (numbers)
  - `majorAxisX`, `majorAxisY` (numbers)
  - `radiusRatio` (number)
  - `layer` (string, optional)

---

## 4. Modification & Editing Tools

### `move_entity`
- **Description**: Moves an entity by base point or displacement vector.
- **Parameters**:
  - `handle` (string, required - or "selected")
  - `fromX`, `fromY`, `toX`, `toY` (numbers)

### `copy_entity`
- **Description**: Duplicates an entity to a target destination.
- **Parameters**:
  - `handle` (string, required)
  - `fromX`, `fromY`, `toX`, `toY` (numbers)

### `rotate_entity`
- **Description**: Rotates an entity around a base point by an angle in degrees.
- **Parameters**:
  - `handle` (string, required)
  - `basePointX`, `basePointY` (numbers)
  - `angleDegrees` (number)

### `scale_entity`
- **Description**: Scales an entity uniformly around a base point.
- **Parameters**:
  - `handle` (string, required)
  - `basePointX`, `basePointY` (numbers)
  - `scaleFactor` (number > 0)

### `mirror_entity`
- **Description**: Mirrors an entity across a mirror axis line.
- **Parameters**:
  - `handle` (string, required)
  - `axisPt1X`, `axisPt1Y`, `axisPt2X`, `axisPt2Y` (numbers)
  - `eraseSource` (boolean, optional)

### `erase_entity` *(Requires Confirmation)*
- **Description**: Permanently removes entities from the drawing.
- **Parameters**:
  - `handle` (string, optional)
  - `handles` (array of strings, optional)

### `fillet_entity`
- **Description**: Applies a fillet radius between two entities.
- **Parameters**:
  - `handle1`, `handle2` (strings, required)
  - `radius` (number, required)

### `offset_entity`
- **Description**: Offsets a curve by a specified distance.
- **Parameters**:
  - `handle` (string, required)
  - `distance` (number, required)
  - `sideX`, `sideY` (numbers, optional)

### `trim_entity` & `extend_entity`
- **Description**: Trims or extends entities relative to boundary edges.

---

## 5. Dimensioning & Text Tools

### `create_linear_dimension`
- **Description**: Adds horizontal or vertical linear dimension.
- **Parameters**:
  - `pt1X`, `pt1Y`, `pt2X`, `pt2Y` (numbers)
  - `textLocationX`, `textLocationY` (numbers)
  - `isHorizontal` (boolean, default true)

### `create_aligned_dimension`
- **Description**: Adds aligned dimension parallel to two points.

### `create_radius_dimension` & `create_diameter_dimension`
- **Description**: Adds radial (R) or diametric (Ø) dimension to circle or arc.

### `create_angular_dimension`
- **Description**: Adds angular dimension between two rays.

### `create_text` & `create_mtext`
- **Description**: Creates single-line or multiline formatted text.

---

## 6. View & Viewport Tools

### `zoom_extents`
- **Description**: Zooms drawing viewport to full extents.

### `zoom_entity`
- **Description**: Zooms viewport to frame a specific entity handle.

### `zoom_window`
- **Description**: Zooms viewport to rectangular coordinate area.
