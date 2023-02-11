import {Component, EventEmitter, Inject, OnDestroy, OnInit, Output} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {OrbitControls} from "three/examples/jsm/controls/OrbitControls";
import * as THREE from "three";
import {RoomEnvironment} from "three/examples/jsm/environments/RoomEnvironment";
import {GemCadFileData} from "../models/gem-cad-file-data";
import {Triangle} from "../models/triangle";
import {PolygonVertex} from "../models/polygon-vertex";

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit, OnDestroy {

  private isInitialized: boolean = false;
  private rendererHostElement: HTMLElement = <any> undefined;
  private orbitControls: OrbitControls = <any> undefined;
  private scene: THREE.Scene = <any> undefined;
  private world: THREE.Group = <any> undefined;
  private lights: THREE.Light[] = [];
  private perspectiveCamera: THREE.PerspectiveCamera = <any> undefined;
  private renderer: THREE.WebGLRenderer = <any> undefined;
  private environmentTexture: THREE.Texture = <any> undefined;
  private sceneBoundsInWorldSpace: THREE.Box3 = new THREE.Box3();
  private sceneBoundingSphereDiameterInWorldSpace: number = 0;
  private sceneSizeInWorldSpace: THREE.Vector3 = new THREE.Vector3();
  private sceneCenterInWorldSpace: THREE.Vector3 = <any> undefined;
  private lastResize: Date = new Date();
  private animationEnabled: boolean = true;

  @Output()
  public onFileDataReady = new EventEmitter<GemCadFileData>();

  public constructor(
    private readonly http: HttpClient,
    @Inject('BASE_URL')
    private readonly baseUrl: string) {
    this.animate = this.animate.bind(this);
    this.handleResize = this.handleResize.bind(this);
  }

  public ngOnInit(): void {
    this.initializeDesignerAsync().then(() => {
      console.log('initialized!');
      this.isInitialized = true;
      this.startAnimation();
    }).catch(err => console.error(err));
  }

  public ngOnDestroy(): void {
    this.dispose();
  }

  public loadFile(files: FileList | null | undefined): void {
    if (files?.length === 1) {
      const formData = new FormData();
      formData.append('file', files[0]);
      this.http.post(this.baseUrl + 'api/loadgemmodel', formData).subscribe((result: any) => {
        this.world.clear();
        const mesh = this.buildPolygonMesh(result);
        this.world.add(mesh);
        console.log('model added to world');
        this.calculateSceneExtents();
        this.resetViewToSceneExtents();
      }, error => console.error(error));
    }
  }

  private buildPolygonMesh(fileData: GemCadFileData): THREE.Mesh {
    const geometry = new THREE.BufferGeometry();

    const positions: number[] = [];
    const normals: number[] = [];
    fileData.tiers.forEach(tier => {
      if (!tier.isPreform) {
        tier.indices.forEach(index => {
          index.renderingTriangles.forEach((t: Triangle) => {
            t.vertices.forEach((pv: PolygonVertex) => {
              positions.push(pv.vertex.x, pv.vertex.y, pv.vertex.z);
              normals.push(pv.normal.x, pv.normal.y, pv.normal.z);
            });
          });
        });
      }
    });
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
    geometry.setAttribute('normal', new THREE.Float32BufferAttribute(normals, 3));
    geometry.computeBoundingBox();
    geometry.computeBoundingSphere();

    var material = new THREE.MeshPhysicalMaterial({
      emissive: 'blue',
      emissiveIntensity: 0.25,
      flatShading: false,
      side: THREE.FrontSide,
      color: 'blue',
      transparent: false,
      reflectivity: 0.50,
      clearcoat: 0.50,
      wireframe: false,
      envMap: this.environmentTexture,
      envMapIntensity: 0.50
    });
    return new THREE.Mesh(geometry, material);
  }

  private async initializeDesignerAsync(): Promise<void> {
    this.rendererHostElement = <any> document.getElementById('renderer-host');
    window.addEventListener('resize', this.handleResize);
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color( 0x000000 ); //   0xCCCCCC 0x000000
    this.world = new THREE.Group();
    this.scene.add(this.world);
    const width = this.rendererHostElement.clientWidth;
    const height = this.rendererHostElement.clientHeight;
    const aspect = this.calcAspect(width, height);
    this.perspectiveCamera = new THREE.PerspectiveCamera(75, aspect, 0.1, 1000);
    this.scene.add(this.perspectiveCamera);
    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.renderer.setPixelRatio( window.devicePixelRatio );
    this.renderer.setSize(width, height, true);
    this.renderer.physicallyCorrectLights = true;
    this.renderer.outputEncoding = THREE.sRGBEncoding;
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.VSMShadowMap;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1;
    this.rendererHostElement.appendChild(this.renderer.domElement);
    this.environmentTexture = await this.loadEnvironmentTexture();
    this.scene.environment = this.environmentTexture;
    this.configureLightingModel('TWO');
    this.orbitControls = this.setupOrbitControls();
    this.updateBounds();
  }

  private dispose(): void {
    this.isInitialized = false;
    window.removeEventListener('resize', this.handleResize);
    this.rendererHostElement.removeChild(this.renderer.domElement);
    this.orbitControls.dispose();
    this.world.clear();
    this.scene.clear();
    this.disposeLights();
    this.renderer.dispose();
    this.environmentTexture?.dispose();
  }

  private disposeLights(): void {
    if (this.lights.length) {
      this.scene.remove(...this.lights);
      this.lights.forEach(l => {
        l.dispose();
      });
      this.lights = [];
    }
  }

  private configureLightingModel(scheme: string): void {
    this.disposeLights();

    switch(scheme) {
      case 'TWO':
        this.configureLightSchemeTwo();
        break;
      default:
        throw new Error(`Unknown lighting scheme: ${scheme}`);
    }

    this.perspectiveCamera.add(...this.lights);
  }

  private configureLightSchemeTwo(): void {
    const ambientLight  = new THREE.AmbientLight(0xFFFFFF, 0.3);
    ambientLight.name = 'LIGHT_AMBIENT_01';
    this.lights.push(ambientLight);

    const directionalLight1  = new THREE.DirectionalLight(0xFFFFFF, 0.8 * Math.PI);
    directionalLight1.position.set(0.5, 0, 0.866); // ~60º
    directionalLight1.name = 'LIGHT_DIRECTIONAL_01';
    directionalLight1.castShadow = true;
    this.lights.push(directionalLight1);
  }

  private async loadEnvironmentTexture(): Promise<THREE.Texture> {
    const pmremGenerator = new THREE.PMREMGenerator( this.renderer );
    pmremGenerator.compileEquirectangularShader();
    return pmremGenerator.fromScene( new RoomEnvironment() ).texture;
  }

  private setupOrbitControls(): OrbitControls {
    const orbitControls = new OrbitControls(this.perspectiveCamera, this.rendererHostElement);
    orbitControls.autoRotate = false;
    orbitControls.enabled = true;
    orbitControls.target = new THREE.Vector3();
    return orbitControls;
  }

  private startAnimation(): void {
    this.animationEnabled = true;
    this.animate();
  }

  private animate(): void {
    if (this.isInitialized && this.animationEnabled) {
      this.orbitControls.update();
      this.redrawScene();
      requestAnimationFrame(this.animate);
    }
  }

  private redrawScene(): void {
    this.renderer.render(this.scene, this.perspectiveCamera);
  }

  private calcAspect(width: number, height: number): number {
    if (height > 0) {
      return width / height;
    }
    return 1;
  }

  private handleResize(e: UIEvent): void {
    this.animationEnabled = false;
    this.lastResize = new Date();
    const compareTime = this.lastResize.valueOf();
    setTimeout(() => {
      if (this.lastResize.valueOf() === compareTime) {
        this.updateBounds();
        this.startAnimation();
      }
    }, 250);
  }

  private updateBounds(): void {
    if (this.renderer) {
      this.renderer.setPixelRatio( window.devicePixelRatio );
      const viewerScreenBounds = this.rendererHostElement.getBoundingClientRect();
      this.renderer.setSize(viewerScreenBounds.width, viewerScreenBounds.height, true);

      const aspect = this.calcAspect(viewerScreenBounds.width, viewerScreenBounds.height);
      this.updatePerspectiveView(aspect);

      this.orbitControls?.update();
    }
  }

  private updatePerspectiveView(aspect: number): void {
    this.perspectiveCamera.aspect = aspect;
    this.perspectiveCamera.updateProjectionMatrix();
  }

  private positionAndAimPerspectiveCamera(): void {
    const fov = this.perspectiveCamera.fov * ( Math.PI / 180 );
    const fovh = 2*Math.atan(Math.tan(fov/2) * this.perspectiveCamera.aspect);
    let dx = this.sceneSizeInWorldSpace.z / 2 + Math.abs( this.sceneSizeInWorldSpace.x / 2 / Math.tan( fovh / 2 ) );
    let dy = this.sceneSizeInWorldSpace.z / 2 + Math.abs( this.sceneSizeInWorldSpace.y / 2 / Math.tan( fov / 2 ) );
    let cameraZ = Math.max(dx, dy);
    this.perspectiveCamera.position.set( this.sceneCenterInWorldSpace.x, this.sceneCenterInWorldSpace.y, cameraZ );
    this.perspectiveCamera.far = this.sceneBoundingSphereDiameterInWorldSpace * 100;
    this.perspectiveCamera.near = this.sceneBoundingSphereDiameterInWorldSpace / 100;
    this.perspectiveCamera.updateProjectionMatrix();
  }

  private calculateSceneExtents(): void {
    this.sceneBoundsInWorldSpace = new THREE.Box3().setFromObject(this.world, true);
    this.sceneBoundingSphereDiameterInWorldSpace = this.sceneBoundsInWorldSpace.getBoundingSphere(new THREE.Sphere()).radius * 2;
    this.sceneCenterInWorldSpace = this.sceneBoundsInWorldSpace.getCenter(new THREE.Vector3());
    this.sceneSizeInWorldSpace = this.sceneBoundsInWorldSpace.getSize(new THREE.Vector3());
  }

  private resetViewToSceneExtents(): void {

    const viewerScreenBounds = this.rendererHostElement.getBoundingClientRect();

    const aspect = this.calcAspect(viewerScreenBounds.width, viewerScreenBounds.height);
    this.updatePerspectiveView(aspect);
    this.positionAndAimPerspectiveCamera();

    this.orbitControls.update();
    this.orbitControls.target = this.sceneCenterInWorldSpace.clone();

    this.redrawScene();
  }
}
