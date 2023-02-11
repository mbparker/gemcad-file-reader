import {Vertex3D} from "./vertex-3d";
import {Triangle} from "./triangle";

export class GemCadFileMetadata {
  public gear: number = 0;
  public gearLocationAngle: number = 0;
  public refractiveIndex: number = 0;
  public symmetryFolds: number = 0;
  public symmetryMirror: boolean = false;
  public headers: string[] = [];
  public footnotes: string[] = [];
}

export class GemCadFileTierIndexData {
  public tier: number = 0;
  public name: string = '';
  public index: number = 0;
  public facetNormal: Vertex3D = new Vertex3D();
  public points: Vertex3D[] = [];
  public renderingTriangles: Triangle[] = [];
}

export class GemCadFileTierData {
  public isPreform: boolean = false;
  public number: number = 0;
  public angle: number = 0;
  public distance: number = 0;
  public cuttingInstructions: string = '';
  public indices: GemCadFileTierIndexData[] = [];
}

export class GemCadFileData {
  public metadata: GemCadFileMetadata = new GemCadFileMetadata();
  public tiers: GemCadFileTierData[] = [];
}
