import {Polygon} from "./polygon";
import {PolygonVertex} from "./polygon-vertex";

export class Triangle extends Polygon {
  public get p1(): PolygonVertex
  {
    return this.vertices[0];
  }

  public set p1(v: PolygonVertex)
  {
    this.vertices[0] = v;
  }

  public get p2(): PolygonVertex
  {
    return this.vertices[1];
  }

  public set p2(v: PolygonVertex)
  {
    this.vertices[1] = v;
  }

  public get p3(): PolygonVertex
  {
    return this.vertices[2];
  }

  public set p3(v: PolygonVertex)
  {
    this.vertices[2] = v;
  }
}
