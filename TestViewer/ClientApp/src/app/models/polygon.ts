import {PolygonVertex} from "./polygon-vertex";
import {Vertex3D} from "./vertex-3d";

export class Polygon {
  public vertices: PolygonVertex[] = [];
  public normal: Vertex3D = new Vertex3D();
  public tag: string = '';
}
